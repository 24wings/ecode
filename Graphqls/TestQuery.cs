using System.Net;
using System.Security.Claims;
using ecode.Data;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Instrumentation;
using GraphQL.Transport;
using GraphQL.Types;

namespace ecode.Graphqls;

public class DroidType : ObjectGraphType<Droid>
{
    public DroidType()
    {
        Name = "Droid";
        Description = "A mechanical creature in the Star Wars universe.";
        Field(d => d.Name, nullable: true).Description("The name of the droid.");
        Field(d => d.Id, nullable: true).Description("The id of the droid.");

        // Field<ListGraphType<EpisodeEnum>>("appearsIn").Description("Which movie they appear in.");
    }
}
public class BaseClassType : ObjectGraphType<BaseClass2>
{
    // public int org_id { get; set; }
    public BaseClassType()
    {
        Name = "BaseClass2";
        Description = "A mechanical creature in the Star Wars universe.";
        // Field(d => d.org_id, nullable: true).Description("The id of the droid.");
        Field(d => d.Id).Description("The id of the droid.");


        // Field<ListGraphType<EpisodeEnum>>("appearsIn").Description("Which movie they appear in.");
    }

}
public class StarWarsQuery : ObjectGraphType
{
    // private readonly DataContext _dataContext;
    public StarWarsQuery()
    {
        // _dataContext = dataContext;
        Field<DroidType>("hero")
          .Resolve(context => new Droid { Name = "R2-D2" });
        Field<DroidType>("heros2")
        .Argument<IdGraphType>("id")
        .Resolve(ctx =>
        {
            var id = ctx.GetArgument<string>("id");
            return new Droid { Id = id, Name = id };

        });
        Field<BaseClassType>("base_class")
        .Resolve(ctx =>
        {
            // var _dataContext = new DataContext();
            return new BaseClass2 { Id = "qqq" };
        }

            );
    }
}

public class StarWarsSchema : Schema
{
    public StarWarsSchema(IServiceProvider provider)
        : base(provider)
    {
        Query = (StarWarsQuery)provider.GetService(typeof(StarWarsQuery)) ?? throw new InvalidOperationException();
        // Mutation = (StarWarsMutation)provider.GetService(typeof(StarWarsMutation)) ?? throw new InvalidOperationException();

        FieldMiddleware.Use(new InstrumentFieldsMiddleware());
    }
}

public class GraphQLSettings
{
    public PathString Path { get; set; } = "/api/graphql";

    public Func<HttpContext, IDictionary<string, object>> BuildUserContext { get; set; }

    public bool EnableMetrics { get; set; }
}



public class GraphQLMiddleware : IMiddleware
{
    private readonly GraphQLSettings _settings;
    private readonly IDocumentExecuter<ISchema> _executer;
    private readonly IGraphQLSerializer _serializer;

    public GraphQLMiddleware(
        GraphQLSettings settings,
        IDocumentExecuter<ISchema> executer,
        IGraphQLSerializer serializer)
    {
        _settings = settings;
        _executer = executer;
        _serializer = serializer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!IsGraphQLRequest(context))
        {
            await next(context);
            return;
        }

        await ExecuteAsync(context);
    }

    private bool IsGraphQLRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(_settings.Path)
            && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
    }
    public class GraphQLUserContext : Dictionary<string, object>
    {
        public ClaimsPrincipal User { get; set; }
    }

    private async Task ExecuteAsync(HttpContext context)
    {
        var request = await _serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted);

        var start = DateTime.UtcNow;

        var result = await _executer.ExecuteAsync(_ =>
        {
            _.Query = request?.Query;
            _.OperationName = request?.OperationName;
            _.Variables = request?.Variables;
            _.UserContext = _settings.BuildUserContext?.Invoke(context);
            _.EnableMetrics = _settings.EnableMetrics;
            _.RequestServices = context.RequestServices;
            _.CancellationToken = context.RequestAborted;
        });

        if (_settings.EnableMetrics)
        {
            result.EnrichWithApolloTracing(start);
        }

        await WriteResponseAsync(context, result);
    }

    private async Task WriteResponseAsync(HttpContext context, ExecutionResult result)
    {
        context.Response.ContentType = "application/graphql+json";
        context.Response.StatusCode = result.Executed ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;

        await _serializer.WriteAsync(context.Response.Body, result, context.RequestAborted);
    }
}


public class StarWarsData
{
    private readonly List<Human> _humans = new List<Human>();
    private readonly List<Droid> _droids = new List<Droid>();

    public StarWarsData()
    {
        _humans.Add(new Human
        {
            Id = "1",
            Name = "Luke",
            Friends = new[] { "3", "4" },
            AppearsIn = new[] { 4, 5, 6 },
            HomePlanet = "Tatooine"
        });
        _humans.Add(new Human
        {
            Id = "2",
            Name = "Vader",
            AppearsIn = new[] { 4, 5, 6 },
            HomePlanet = "Tatooine"
        });

        _droids.Add(new Droid
        {
            Id = "3",
            Name = "R2-D2",
            Friends = new[] { "1", "4" },
            AppearsIn = new[] { 4, 5, 6 },
            PrimaryFunction = "Astromech"
        });
        _droids.Add(new Droid
        {
            Id = "4",
            Name = "C-3PO",
            AppearsIn = new[] { 4, 5, 6 },
            PrimaryFunction = "Protocol"
        });
    }

    public IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character)
    {
        if (character == null)
        {
            return null;
        }

        var friends = new List<StarWarsCharacter>();
        var lookup = character.Friends;
        if (lookup != null)
        {
            foreach (var h in _humans.Where(h => lookup.Contains(h.Id)))
                friends.Add(h);
            foreach (var d in _droids.Where(d => lookup.Contains(d.Id)))
                friends.Add(d);
        }
        return friends;
    }

    public Task<Human> GetHumanByIdAsync(string id)
    {
        return Task.FromResult(_humans.FirstOrDefault(h => h.Id == id));
    }

    public Task<Droid> GetDroidByIdAsync(string id)
    {
        return Task.FromResult(_droids.FirstOrDefault(h => h.Id == id));
    }

    public Human AddHuman(Human human)
    {
        human.Id = Guid.NewGuid().ToString();
        _humans.Add(human);
        return human;
    }
}


public abstract class StarWarsCharacter
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string[] Friends { get; set; }
    public int[] AppearsIn { get; set; }
}

public class Human : StarWarsCharacter
{
    public string HomePlanet { get; set; }
}

public class Droid : StarWarsCharacter
{
    public string PrimaryFunction { get; set; }
}


public class MySchema : Schema
{
    public MySchema(IServiceProvider services) : base(services)
    {
    }
}
public class Order
{
    public int id { get; set; }
    public string name { get; set; }
}
