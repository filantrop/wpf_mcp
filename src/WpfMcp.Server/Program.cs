using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using WpfMcp.Server.Services;
using WpfMcp.Server.Tools;

namespace WpfMcp.Server;

/// <summary>
/// WPF-MCP Server entry point.
/// Provides MCP tools for automating WPF applications using UI Automation.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Register services
        builder.Services.AddSingleton<IApplicationManager, ApplicationManager>();
        builder.Services.AddSingleton<IElementReferenceManager, ElementReferenceManager>();

        // Register MCP tool classes
        builder.Services.AddSingleton<WpfApplicationTools>();
        builder.Services.AddSingleton<WpfSnapshotTools>();
        builder.Services.AddSingleton<WpfInteractionTools>();
        builder.Services.AddSingleton<WpfNavigationTools>();
        builder.Services.AddSingleton<WpfWindowTools>();
        builder.Services.AddSingleton<WpfUtilityTools>();

        // Configure MCP server
        builder.Services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "wpf-mcp",
                Version = "1.0.0"
            };
        })
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(Program).Assembly);

        var host = builder.Build();

        await host.RunAsync();
    }
}
