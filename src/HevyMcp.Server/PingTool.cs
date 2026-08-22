namespace HevyMcp.Server;

using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class PingTool
{
    [McpServerTool(Name = "hevy_ping")]
    [Description("Returns a fixed number. Health check to verify the MCP server is alive.")]
    public static int Ping() => 42;
}