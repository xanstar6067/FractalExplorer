using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics; // Необходимо для вывода отладочных сообщений
using System.Linq;

namespace CPU_Benchmark
{
    /// <summary>
    /// Предоставляет информацию о накопителях, включая их имя и температуру.
    /// </summary>
    public readonly struct StorageInfo
    {
        /// <summary>
        /// Имя (модель) накопителя.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Температура накопителя в градусах Цельсия. Может быть null, если сенсор недоступен.
        /// </summary>
        public float? Temperature { get; }

        /// <summary>
        /// Инициализирует новый экземпляр структуры <see cref="StorageInfo"/>.
        /// </summary>
        /// <param name="name">Имя накопителя.</param>
        /// <param name="temp">Температура накопителя.</param>
        public StorageInfo(string name, float? temp)
        {
            Name = name;
            Temperature = temp;
        }
    }

    /// <summary>
    /// Обертка для работы с библиотекой LibreHardwareMonitor.
    /// Предоставляет доступ к динамическим данным с аппаратных сенсоров (ЦП, накопители).
    /// Реализует IDisposable для корректного освобождения ресурсов.
    /// </summary>
    public class SystemMonitor : IDisposable
    {
        /// <summary>
        /// Основной объект LibreHardwareMonitor, управляющий доступом к оборудованию.
        /// </summary>
        private readonly Computer _computer;

        /// <summary>
        /// Кэшированная ссылка на аппаратный объект ЦП для быстрого доступа.
        /// </summary>
        private IHardware? _cpu;

        /// <summary>
        /// Кэшированный список аппаратных объектов накопителей.
        /// </summary>
        private readonly List<IHardware> _storageDevices = new();

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SystemMonitor"/>,
        /// настраивая его на мониторинг ЦП и накопителей.
        /// </summary>
        public SystemMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsStorageEnabled = true
            };
        }

        /// <summary>
        /// Открывает соединение с оборудованием и находит необходимые устройства (ЦП и накопители).
        /// Этот метод должен быть вызван перед получением любых данных с сенсоров.
        /// </summary>
        public void Initialize()
        {
            try
            {
                _computer.Open();
                _cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                _storageDevices.AddRange(_computer.Hardware.Where(h => h.HardwareType == HardwareType.Storage));
            }
            catch (Exception ex)
            {
                // Это сообщение появится в окне "Вывод" -> "Отладка" в Visual Studio
                Debug.WriteLine("ОШИБКА: Не удалось инициализировать SystemMonitor. Мониторинг оборудования будет недоступен.");
                Debug.WriteLine($"ПРИЧИНА: {ex.Message}");
                // Если антивирус удалил DLL, здесь, скорее всего, будет ошибка 'FileNotFoundException'
                // или 'BadImageFormatException', указывающая на LibreHardwareMonitorLib.dll.
            }
        }

        /// <summary>
        /// Принудительно обновляет данные всех сенсоров на указанном устройстве.
        /// </summary>
        /// <param name="hardware">Аппаратный компонент для обновления.</param>
        private void UpdateHardware(IHardware hardware)
        {
            hardware?.Update();
        }

        /// <summary>
        /// Получает МАКСИМАЛЬНУЮ температуру процессора, анализируя все ключевые сенсоры (Tdie, CCDs).
        /// Этот метод надежно работает с многочиповыми процессорами AMD Ryzen, включая Zen 4 и Zen 5.
        /// </summary>
        /// <returns>Максимальная температура в градусах Цельсия или <c>null</c>, если сенсоры не найдены.</returns>
        public float? GetCpuTemperature()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);

            // 1. Основной, самый надежный способ для современных AMD Ryzen.
            // Находим ВСЕ релевантные температурные сенсоры. Ищем сенсоры,
            // в имени которых есть "Tdie" (температура кристалла) или "CCD" (температура чиплета).
            var amdTempSensors = _cpu.Sensors.Where(s =>
                s.SensorType == SensorType.Temperature &&
                s.Value.HasValue &&
                (s.Name.Contains("Tdie") || s.Name.Contains("CCD")));

            // 2. Если такие сенсоры найдены, возвращаем МАКСИМАЛЬНОЕ значение среди них.
            // Это дает самую точную картину пиковой температуры процессора.
            if (amdTempSensors.Any())
            {
                return amdTempSensors.Max(s => s.Value);
            }

            // 3. Запасной вариант (Fallback) для Intel или старых процессоров AMD.
            // Если специфичных сенсоров Tdie/CCD нет, ищем общий "Package" или "Core" сенсор.
            var fallbackSensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature &&
                s.Value.HasValue &&
                (s.Name.Contains("Package") || s.Name.Contains("Core")));

            return fallbackSensor?.Value;
        }

        /// <summary>
        /// Получает общую загрузку процессора.
        /// </summary>
        /// <returns>Загрузка в процентах или <c>null</c>, если сенсор не найден.</returns>
        public float? GetCpuTotalLoad()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);
            var sensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Load && s.Name.Contains("CPU Total"));
            return sensor?.Value;
        }

        /// <summary>
        /// Получает энергопотребление процессорного пакета.
        /// Поиск ведется по нескольким возможным именам сенсоров для повышения надежности.
        /// </summary>
        /// <returns>Мощность в ваттах или <c>null</c>, если подходящий сенсор не найден.</returns>
        public float? GetCpuPackagePower()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);

            var priorityNames = new[] { "CPU Package", "CPU Socket" };

            foreach (var name in priorityNames)
            {
                var sensor = _cpu.Sensors.FirstOrDefault(s =>
                    s.SensorType == SensorType.Power && s.Name.Contains(name));

                if (sensor?.Value != null)
                {
                    return sensor.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Получает список всех обнаруженных накопителей и их температур.
        /// </summary>
        /// <returns>Список объектов <see cref="StorageInfo"/>.</returns>
        public List<StorageInfo> GetStorageInfo()
        {
            var result = new List<StorageInfo>();
            foreach (var device in _storageDevices)
            {
                UpdateHardware(device);
                string name = device.Name;
                var tempSensor = device.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                result.Add(new StorageInfo(name, tempSensor?.Value));
            }
            return result;
        }

        /// <summary>
        /// Освобождает ресурсы, используемые <see cref="SystemMonitor"/>,
        /// закрывая соединение с LibreHardwareMonitor.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _computer.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА: Произошла ошибка при освобождении ресурсов SystemMonitor: {ex.Message}");
            }
        }
    }
}