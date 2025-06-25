using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq; // Хотя явно не используется, часто требуется для IEnumerable
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Resources
{
    /// <summary>
    /// Управляет параллельным рендерингом плиток (тайлов), распределяя их по рабочим потокам.
    /// </summary>
    public class TileRenderDispatcher
    {
        #region Fields

        /// <summary>
        /// Коллекция плиток, которые необходимо отрендерить.
        /// Порядок плиток в этой коллекции определяет порядок их обработки,
        /// что может быть важно для прогрессивного рендеринга.
        /// </summary>
        private readonly IEnumerable<TileInfo> _tilesToRender;

        /// <summary>
        /// Максимальное количество одновременно работающих потоков (задач) для рендеринга.
        /// </summary>
        private readonly int _maxConcurrency;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TileRenderDispatcher"/>.
        /// </summary>
        /// <param name="tiles">Коллекция плиток для рендеринга. Порядок, в котором они предоставлены, важен для последовательности обработки.</param>
        /// <param name="maxConcurrency">Максимальное количество одновременно работающих потоков/задач. Должно быть не менее 1.</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="tiles"/> равно null.</exception>
        public TileRenderDispatcher(IEnumerable<TileInfo> tiles, int maxConcurrency)
        {
            _tilesToRender = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _maxConcurrency = Math.Max(1, maxConcurrency); // Гарантируем, что минимум один поток будет работать
        }

        #endregion

        #region Methods

        /// <summary>
        /// Асинхронно запускает процесс рендеринга всех плиток, распределяя их по доступным потокам.
        /// </summary>
        /// <param name="renderAction">Действие, которое нужно выполнить для каждой плитки.
        /// Принимает объект <see cref="TileInfo"/> (информация о плитке) и <see cref="CancellationToken"/> (токен отмены)
        /// и возвращает <see cref="Task"/>, представляющий асинхронную операцию рендеринга плитки.</param>
        /// <param name="token">Токен для отмены операции. Если отмена запрошена, рендеринг будет прерван.</param>
        /// <returns>Задача, которая завершится после того, как все плитки будут обработаны или операция будет отменена.</returns>
        public async Task RenderAsync(Func<TileInfo, CancellationToken, Task> renderAction, CancellationToken token)
        {
            // Используем потокобезопасную очередь для управления задачами плиток.
            var tileQueue = new ConcurrentQueue<TileInfo>(_tilesToRender);
            var tasks = new List<Task>();

            // Запускаем ограниченное количество параллельных задач (рабочих потоков).
            for (int i = 0; i < _maxConcurrency; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Каждый рабочий поток извлекает плитки из очереди, пока она не станет пустой.
                    while (tileQueue.TryDequeue(out TileInfo tile))
                    {
                        if (token.IsCancellationRequested)
                        {
                            // Если отмена запрошена, текущая задача завершается.
                            return;
                        }
                        await renderAction(tile, token);
                    }
                }, token)); // Передаем токен отмены в каждую задачу
            }

            // Ожидаем завершения всех запущенных рабочих потоков.
            await Task.WhenAll(tasks);
        }

        #endregion
    }
}