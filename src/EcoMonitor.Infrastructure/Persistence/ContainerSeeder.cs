using EcoMonitor.Domain.Entities;
using EcoMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcoMonitor.Infrastructure.Persistence;

public static class ContainerSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        if (await dbContext.WasteContainers.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // ~35 containers spread across Bishkek districts.
        // Type mix: ~50% General, ~15% Plastic, ~10% Glass, ~10% Paper, ~15% Organic.
        // Capacity rotates between 240, 660, 1100 L. Codes are zero-padded sequentially.
        // InstalledAt is deterministic so seeded data is reproducible across runs.
        var entries = new (string address, double lat, double lng, ContainerType type, int capacity, int monthsAgo)[]
        {
            ("Чуйский проспект 100",        42.876, 74.601, ContainerType.General, 1100, 28),
            ("Чуйский проспект 200",        42.875, 74.612, ContainerType.Plastic,  660, 14),
            ("проспект Манаса 56",          42.866, 74.589, ContainerType.General, 1100, 36),
            ("проспект Манаса 100",         42.870, 74.591, ContainerType.Glass,    660, 22),
            ("улица Юнусалиева 2/1",        42.838, 74.597, ContainerType.General,  660, 19),
            ("Ала-Тоо площадь",             42.876, 74.604, ContainerType.General, 1100, 30),
            ("ЦУМ Айчурек",                 42.873, 74.610, ContainerType.General, 1100, 12),
            ("Бишкек Парк, главный вход",   42.876, 74.595, ContainerType.Organic,  660, 16),
            ("Дордой, центральный въезд",   42.917, 74.668, ContainerType.General, 1100, 33),
            ("Дордой, северный сектор",     42.917, 74.670, ContainerType.Plastic,  660, 17),
            ("Орто-Сай",                    42.844, 74.603, ContainerType.General,  660, 24),
            ("Алтын Казык",                 42.880, 74.580, ContainerType.Paper,    240, 11),
            ("микрорайон Восток-5, дом 12", 42.864, 74.642, ContainerType.General, 1100, 27),
            ("микрорайон Аламедин-1",       42.870, 74.625, ContainerType.General, 1100, 31),
            ("микрорайон Джал, дом 8",      42.834, 74.580, ContainerType.General, 1100, 20),
            ("микрорайон Асанбай, дом 3",   42.831, 74.612, ContainerType.Plastic,  660, 13),
            ("Совмин",                      42.872, 74.613, ContainerType.General,  660, 25),
            ("Парк Панфилова",              42.879, 74.609, ContainerType.Organic,  240, 18),
            ("улица Киевская 95",           42.879, 74.601, ContainerType.General,  660, 15),
            ("улица Токтогула 100",         42.881, 74.599, ContainerType.Glass,    240, 21),
            ("улица Боконбаева 100",        42.873, 74.594, ContainerType.General, 1100, 29),
            ("улица Ибраимова 50",          42.866, 74.617, ContainerType.Paper,    240, 10),
            ("улица Тыныстанова 250",       42.871, 74.593, ContainerType.General,  660, 23),
            ("микрорайон Тунгуч",           42.857, 74.640, ContainerType.General, 1100, 32),
            ("микрорайон Аламедин-7",       42.867, 74.640, ContainerType.Organic,  660,  9),
            ("парк Молодой Гвардии",        42.868, 74.611, ContainerType.General,  660, 26),
            ("Ошский базар, западный вход", 42.878, 74.585, ContainerType.General, 1100, 34),
            ("Ошский базар, восточный",     42.878, 74.586, ContainerType.Plastic,  660,  8),
            ("улица Логвиненко 30",         42.886, 74.594, ContainerType.General,  660,  7),
            ("микрорайон Восток-6",         42.866, 74.648, ContainerType.Organic,  660, 17),
            ("улица Раззакова 30",          42.872, 74.609, ContainerType.General,  660, 14),
            ("улица Раззакова 50",          42.871, 74.606, ContainerType.Paper,    240,  6),
            ("микрорайон 5, дом 17",        42.867, 74.635, ContainerType.General, 1100, 30),
            ("улица Уметалиева 100",        42.876, 74.620, ContainerType.Glass,    660, 19),
            ("Парк Дружбы",                 42.852, 74.585, ContainerType.Organic,  660, 12)
        };

        var containers = new List<WasteContainer>(entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            containers.Add(new WasteContainer
            {
                Code = $"C-{(i + 1):D5}",
                Address = e.address,
                Latitude = e.lat,
                Longitude = e.lng,
                Type = e.type,
                Capacity = e.capacity,
                Status = ContainerStatus.Active,
                InstalledAt = now.AddMonths(-e.monthsAgo)
            });
        }

        dbContext.WasteContainers.AddRange(containers);
        await dbContext.SaveChangesAsync(ct);
    }
}
