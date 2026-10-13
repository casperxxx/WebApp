using WebApp.Domain.Entities;

namespace EventApi.IntegrationTests;

[Collection("Postgres")]
public class EventRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public EventRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static DateTime Utc(int year, int month, int day, int hour = 10) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent(string title, DateTime start, DateTime end, int seats = 10) =>
        Event.Create(title, null, start, end, seats);

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ReturnsEvent()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent("Концерт", Utc(2026, 8, 10), Utc(2026, 8, 10, 12));

            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();
            var loaded = await events.GetByIdAsync(eventItem.Id);

            Assert.NotNull(loaded);
            Assert.Equal(eventItem.Id, loaded.Id);
            Assert.Equal("Концерт", loaded.Title);
            Assert.Equal(10, loaded.AvailableSeats);
        }
    }

    [Fact]
    public async Task AddAsync_UnspecifiedKind_SavesWithoutError()
    {
        // даты без Z / Kind=Unspecified — конвертер в AppDbContext приводит к UTC
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var start = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Unspecified);
            var end = new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Unspecified);
            var eventItem = Event.Create("Без зоны", null, start, end, 5);

            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();
        }

        // новый контекст — иначе трекер отдаст исходную сущность с Kind=Unspecified
        var (events2, _, _, context2) = _fixture.CreateRepositories();
        await using (context2)
        {
            var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var (items, total) = await events2.GetEventsAsync(null, from, null, 0, 10);

            Assert.Equal(1, total);
            Assert.Equal("Без зоны", items.Single().Title);
            Assert.Equal(DateTimeKind.Utc, items.Single().StartAt.Kind);
        }
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, _, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var loaded = await events.GetByIdAsync(Guid.NewGuid());
            Assert.Null(loaded);
        }
    }

    [Fact]
    public async Task Update_ChangesTitleAndSeats()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent("Старое", Utc(2026, 8, 10), Utc(2026, 8, 10, 12), seats: 5);
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            eventItem.Update("Новое", null, Utc(2026, 8, 10), Utc(2026, 8, 10, 12), 8);
            await unitOfWork.SaveChangesAsync();

            var loaded = await events.GetByIdAsync(eventItem.Id);
            Assert.Equal("Новое", loaded!.Title);
            Assert.Equal(8, loaded.TotalSeats);
        }
    }

    [Fact]
    public async Task Delete_RemovesEvent()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            var eventItem = CreateEvent("Удалить", Utc(2026, 8, 10), Utc(2026, 8, 10, 12));
            await events.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            events.Delete(eventItem);
            await unitOfWork.SaveChangesAsync();

            var loaded = await events.GetByIdAsync(eventItem.Id);
            Assert.Null(loaded);
        }
    }

    [Fact]
    public async Task GetEventsAsync_FiltersByTitle()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            await events.AddAsync(CreateEvent("Концерт рок", Utc(2026, 8, 10), Utc(2026, 8, 10, 12)));
            await events.AddAsync(CreateEvent("Встреча", Utc(2026, 8, 11), Utc(2026, 8, 11, 12)));
            await events.AddAsync(CreateEvent("концерт джаз", Utc(2026, 8, 12), Utc(2026, 8, 12, 12)));
            await unitOfWork.SaveChangesAsync();

            var (items, total) = await events.GetEventsAsync("концерт", null, null, 0, 10);

            Assert.Equal(2, total);
            Assert.Equal(2, items.Count);
            Assert.All(items, e => Assert.Contains("концерт", e.Title, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetEventsAsync_FiltersByFromAndTo()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            await events.AddAsync(CreateEvent("Раньше", Utc(2026, 7, 1), Utc(2026, 7, 1, 12)));
            await events.AddAsync(CreateEvent("В диапазоне", Utc(2026, 8, 10), Utc(2026, 8, 10, 12)));
            await events.AddAsync(CreateEvent("Позже", Utc(2026, 9, 1), Utc(2026, 9, 1, 12)));
            await unitOfWork.SaveChangesAsync();

            var from = Utc(2026, 8, 1);
            var to = Utc(2026, 8, 31, 23);
            var (items, total) = await events.GetEventsAsync(null, from, to, 0, 10);

            Assert.Equal(1, total);
            Assert.Equal("В диапазоне", items.Single().Title);
        }
    }

    [Fact]
    public async Task GetEventsAsync_Pagination_ReturnsCorrectPage()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            for (var i = 1; i <= 5; i++)
            {
                await events.AddAsync(CreateEvent($"Событие {i}", Utc(2026, 8, i), Utc(2026, 8, i, 12)));
            }

            await unitOfWork.SaveChangesAsync();

            var (page1, total1) = await events.GetEventsAsync(null, null, null, skip: 0, take: 2);
            var (page2, total2) = await events.GetEventsAsync(null, null, null, skip: 2, take: 2);
            var (page3, total3) = await events.GetEventsAsync(null, null, null, skip: 4, take: 2);

            Assert.Equal(5, total1);
            Assert.Equal(5, total2);
            Assert.Equal(5, total3);
            Assert.Equal(2, page1.Count);
            Assert.Equal(2, page2.Count);
            Assert.Single(page3);
        }
    }

    [Fact]
    public async Task GetEventsAsync_CombinedFiltersAndPagination()
    {
        await _fixture.ResetDatabaseAsync();
        var (events, _, unitOfWork, context) = _fixture.CreateRepositories();
        await using (context)
        {
            await events.AddAsync(CreateEvent("Митинг A", Utc(2026, 8, 1), Utc(2026, 8, 1, 12)));
            await events.AddAsync(CreateEvent("Митинг B", Utc(2026, 8, 2), Utc(2026, 8, 2, 12)));
            await events.AddAsync(CreateEvent("Митинг C", Utc(2026, 8, 3), Utc(2026, 8, 3, 12)));
            await events.AddAsync(CreateEvent("Другое", Utc(2026, 8, 2), Utc(2026, 8, 2, 12)));
            await unitOfWork.SaveChangesAsync();

            var (items, total) = await events.GetEventsAsync(
                "митинг",
                Utc(2026, 8, 1),
                Utc(2026, 8, 31, 23),
                skip: 1,
                take: 1);

            Assert.Equal(3, total);
            Assert.Single(items);
        }
    }
}
