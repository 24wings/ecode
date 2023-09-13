using ecode.Components;
using GraphQL.MicrosoftDI;
using GraphQL;
using GraphQL.Server;
using ecode.Graphqls;
using Microsoft.Extensions.DependencyInjection;

using static ecode.Graphqls.GraphQLMiddleware;
using GraphQL.DI;
using GraphQL.Types;
using GraphQL.DataLoader;
using ecode.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// IGraphQLBuilder
// Add services to the container.
// builder.Services.AddDbContext<DataContext>(opt => opt.UseNpgsql("Host=10.1.7.61;Username=postgres;Password=postgres;Database=postgres"));

builder.Services.AddRazorComponents()
    .AddServerComponents();
builder.Services.AddControllers();
builder.Services.AddGraphQL(b =>
//  b.AddHttpMiddleware<ISchema>()

// .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)

b.AddSchema<StarWarsSchema>()

.AddSystemTextJson()
.AddGraphTypes(typeof(StarWarsSchema).Assembly)

// .AddValidationRule<InputValidationRule>()



);
builder.Services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
builder.Services.AddSingleton<DataLoaderDocumentListener>();
builder.Services.AddSingleton<MySchema>();


// builder.Services
// // .AddMetrics((services, _) => services.GetRequiredService<GraphQLSettings>().EnableMetrics)
//   .AddMetrics();

// builder.Services.AddSingleton<GraphQLMiddleware>();

// builder.Services.AddSingleton(new GraphQLSettings
// {
//     Path = "/graphql",
//     BuildUserContext = ctx => new GraphQLUserContext
//     {
//         User = ctx.User
//     },
//     EnableMetrics = true
// });
builder.Services.AddLogging(builder => builder.AddConsole());
// builder.Services.AddHttpContextAccessor();
// }
// builder.Services.AddSingleton<GraphQLMiddleware>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();

var listener = app.Services.GetRequiredService<DataLoaderDocumentListener>();

var executer = new DocumentExecuter();
var result = executer.ExecuteAsync(opts =>
{

    // ...

    opts.Listeners.Add(listener);
});

app.UseStaticFiles();
// app.UseMiddleware<GraphQLMiddleware>();

app.UseGraphQLGraphiQL();
// app.UseGraphQLAltair();
app.UseGraphQL<ISchema>();
app.UseRouting();


app.UseEndpoints(e =>
{

    e.MapControllers();
    e.MapRazorComponents<App>().AddServerRenderMode();

});

// app.MapRazorComponents<App>()
//     .AddServerRenderMode();

app.Run();
