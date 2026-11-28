using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EduHub.Shared.Events;

public interface IEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}

public class InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                // Quyết định kiến trúc (Phase 5.5): Handler xử lý sự kiện chạy in-memory là side-effect
                // (ví dụ: tạo thông báo tự động). Nếu handler gặp lỗi, chỉ ghi log để theo dõi chứ
                // KHÔNG ném lại exception, đảm bảo luồng nghiệp vụ chính (đã SaveChangesAsync)
                // không bị gián đoạn hoặc trả lỗi 500 cho người dùng.
                logger.LogError(
                    ex,
                    "Lỗi khi xử lý sự kiện {EventType} trong handler {HandlerType}",
                    typeof(TEvent).Name,
                    handler.GetType().Name);
            }
        }
    }
}

