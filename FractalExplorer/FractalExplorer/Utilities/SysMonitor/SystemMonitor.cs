using LibreHardwareMonitor.Hardware;

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
                // Находим все устройства типа Storage и добавляем их в список.
                _storageDevices.AddRange(_computer.Hardware.Where(h => h.HardwareType == HardwareType.Storage));
            }
            catch
            {
                // Игнорируем ошибки инициализации, чтобы приложение могло работать
                // даже если LibreHardwareMonitor не смог запуститься (например, из-за отсутствия прав).
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
        /// Получает температуру процессора, выполняя поиск по списку приоритетных сенсоров
        /// для лучшей совместимости (например, с процессорами Ryzen X3D).
        /// </summary>
        /// <returns>Температура в градусах Цельсия или <c>null</c>, если подходящий сенсор не найден.</returns>
        public float? GetCpuTemperature()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);

            // Расширенный список для поддержки Ryzen 9950X3D и других процессоров
            var priorityNames = new[]
            {
        "Core (Tctl/Tdie)",    // Ryzen 5000/7000/9000 серии (включая X3D)
        "Core (Tctl)",          // Альтернативное имя
        "CCD1 (Tdie)",          // Chiplet 1 без префикса "CPU"
        "CPU CCD1 (Tdie)",      // Старое именование
        "CPU Package",          // Intel
        "Core (Tj)"            // Другие процессоры
    };

            foreach (var name in priorityNames)
            {
                var sensor = _cpu.Sensors.FirstOrDefault(s =>
                    s.SensorType == SensorType.Temperature &&
                    s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (sensor?.Value != null)
                {
                    return sensor.Value;
                }
            }

            // Если точное совпадение не найдено, ищем по частичному совпадению
            var tempSensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature &&
                (s.Name.Contains("Tdie") || s.Name.Contains("Package")));

            return tempSensor?.Value;
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

            // Ищем сенсор мощности по нескольким возможным именам.
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
                // Имя устройства - это и есть его модель
                string name = device.Name;
                // Ищем сенсор температуры
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
            try { 
                _computer.Close();
            } 
            catch { }
        }
    }
}