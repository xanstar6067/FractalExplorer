using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FractalDraving
{
    /// <summary>
    /// Управляет параллельным рендерингом плиток (тайлов), распределяя их по рабочим потокам.
    /// </summary>
    public class TileRenderDispatcher
    {
        private readonly IEnumerable<TileInfo> _tilesToRender;
        private readonly int _maxConcurrency;

        /// <summary>
        /// Конструктор диспетчера.
        /// </summary>
        /// <param name="tiles">Коллекция плиток для рендеринга. Порядок, в котором они здесь, важен.</param>
        /// <param name="maxConcurrency">Максимальное количество одновременно работающих потоков.</param>
        public TileRenderDispatcher(IEnumerable<TileInfo> tiles, int maxConcurrency)
        {
            _tilesToRender = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _maxConcurrency = Math.Max(1, maxConcurrency);
        }

        /// <summary>
        /// Асинхронно запускает процесс рендеринга всех плиток.
        /// </summary>
        /// <param name="renderAction">Действие, которое нужно выполнить для каждой плитки. Принимает TileInfo и CancellationToken.</param>
        /// <param name="token">Токен для отмены операции.</param>
        public async Task RenderAsync(Func<TileInfo, CancellationToken, Task> renderAction, CancellationToken token)
        {
            // Используем потокобезопасную очередь для задач
            var tileQueue = new ConcurrentQueue<TileInfo>(_tilesToRender);
            var tasks = new List<Task>();

            for (int i = 0; i < _maxConcurrency; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Каждый рабочий поток берет задачи из очереди, пока они не закончатся
                    while (tileQueue.TryDequeue(out TileInfo tile))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return; // Прерываем работу, если пришел сигнал отмены
                        }
                        await renderAction(tile, token);
                    }
                }, token));
            }

            // Ожидаем завершения всех рабочих потоков
            await Task.WhenAll(tasks);
        }
    }
}