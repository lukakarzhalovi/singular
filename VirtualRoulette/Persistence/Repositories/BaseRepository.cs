namespace VirtualRoulette.Persistence.Repositories;

public class BaseRepository
{
    protected BaseRepository(AppDbContext context)
    {
        Context = context;
    }

    protected AppDbContext Context { get; private set; }
}
