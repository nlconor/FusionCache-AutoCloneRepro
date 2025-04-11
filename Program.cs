using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(options => { options.EnableAutoClone = true; })
    .WithSerializer(new FusionCacheSystemTextJsonSerializer());

var app = builder.Build();

app.MapGet("/test", async (IFusionCache cache) =>
    {
        var results = new List<Participant>();
        var user = new User
        {
            ID = 100,
            Name = "A User"
        };

        // Retrieve participants from cache
        for (int i = 1; i <= 3; i++)
        {
            var cachedParticipant = await cache.TryGetAsync<Participant>(
                $"participant:{i}",
                options => { options.EnableAutoClone = true; });
            if (cachedParticipant.HasValue)
                results.Add(cachedParticipant.Value);
        }

        if (results.Count < 3)
        {
            // Generate 3 participants
            var participants = new List<Participant>();
            for (int i = 1; i <= 3; i++)
            {
                participants.Add(new Participant
                {
                    ID = i,
                    UserID = user.ID
                });
            }

            // Store participants in cache
            foreach (var participant in participants)
            {
                await cache.SetAsync($"participant:{participant.ID}", participant,
                    options => { options.EnableAutoClone = true; });
            }

            results.AddRange(participants);
        }

        // Set user object on cached participants
        foreach (var participant in results)
        /*
         * `results` entries on the 2nd request here have `User` set on them despite Auto Clone being enabled &
         * the original object being modified after SetAsync was called.
         */
        {
            participant.User = user;
        }

        return Results.Ok(results);
    });

app.Run();

class Participant
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public User User { get; set; }
}

class User
{
    public int ID { get; set; }
    public string Name { get; set; }
}