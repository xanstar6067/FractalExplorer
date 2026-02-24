using System.Collections.Concurrent;

namespace FractalExplorer.Resources
{
    /// <summary>
    /// Стратегия выбора порядка плиток перед распределением по рабочим потокам.
    /// </summary>
    public enum TileSchedulingStrategy
    {
        /// <summary>
        /// Порядок плиток сохраняется таким, каким он был передан в диспетчер.
        /// </summary>
        PreserveInputOrder,

        /// <summary>
        /// Плитки перемешиваются случайным образом перед рендерингом.
        /// </summary>
        Randomized
    }

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

        /// <summary>
        /// Стратегия, по которой будет подготовлен порядок обработки плиток.
        /// </summary>
        private readonly TileSchedulingStrategy _schedulingStrategy;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TileRenderDispatcher"/>.
        /// </summary>
        /// <param name="tiles">Коллекция плиток для рендеринга. Порядок, в котором они предоставлены, важен для последовательности обработки.</param>
        /// <param name="maxConcurrency">Максимальное количество одновременно работающих потоков/задач. Должно быть не менее 1.</param>
        /// <param name="schedulingStrategy">Стратегия распределения порядка плиток перед рендерингом.</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="tiles"/> равно null.</exception>
        public TileRenderDispatcher(
            IEnumerable<TileInfo> tiles,
            int maxConcurrency,
            TileSchedulingStrategy schedulingStrategy = TileSchedulingStrategy.Randomized)
        {
            _tilesToRender = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _maxConcurrency = Math.Max(1, maxConcurrency); // Гарантируем, что минимум один поток будет работать
            _schedulingStrategy = schedulingStrategy;
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
            var preparedTiles = PrepareTileSequence(_tilesToRender, _schedulingStrategy);

            // Используем потокобезопасную очередь для управления задачами плиток.
            var tileQueue = new ConcurrentQueue<TileInfo>(preparedTiles);
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

        /// <summary>
        /// Формирует порядок обработки плиток в зависимости от выбранной стратегии.
        /// </summary>
        private static IReadOnlyList<TileInfo> PrepareTileSequence(
            IEnumerable<TileInfo> sourceTiles,
            TileSchedulingStrategy strategy)
        {
            var tiles = sourceTiles as List<TileInfo> ?? sourceTiles.ToList();
            if (tiles.Count <= 1 || strategy == TileSchedulingStrategy.PreserveInputOrder)
            {
                return tiles;
            }

            var random = Random.Shared;
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }

            return tiles;
        }

        #endregion
    }
}
