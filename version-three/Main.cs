using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReflexionJson.VersionThree;

/*********************************************/
// Entry point
/*********************************************/

public class Main
{
    public static async ValueTask Run()
    {
        QueryService service = new();
        GenericExporter exporter = new();
        TypeEtl typeEtl = new(service, exporter);
        MetricEtl metricsEtl = new(service, exporter);

        await typeEtl.ProcessTypes();
        await metricsEtl.ProcessMetrics();
    }
}

/*********************************************/
// Specific code
/*********************************************/

class TypeEtl(QueryService service, GenericExporter exporter)
{
    public async ValueTask ProcessTypes()
        => await exporter.SaveAsync((await GetTypes()).Select(ProcessType), "types.json");

    public async ValueTask<IEnumerable<DataType>> GetTypes()
        => (await service.GetNodes(path:"types"))
            .Deserialize<DataType>(nodeSelector:"data");

    public DataTypeOutput ProcessType(DataType input)
        => new() { Name = input.Name };
}

class MetricEtl(QueryService service, GenericExporter exporter)
{
    public async ValueTask ProcessMetrics()
        => await exporter.SaveAsync((await GetMetrics()).Select(ProcessMetrics), "metrics.json");

    public async ValueTask<IEnumerable<Metric>> GetMetrics()
        => (await service.GetNodes(path:"metrics"))
            .Deserialize<Metric>(nodeSelector:"informations");

    public MetricOutput ProcessMetrics(Metric input)
        => new() { Description = input.Volume };
}

/*********************************************/
// Generic code 
/*********************************************/
public class GenericExporter
{
    public async ValueTask SaveAsync<T>(IEnumerable<T> data, string path)
    {
        foreach (var item in data)
        {
            Console.WriteLine(item);
        }
        if (!Directory.Exists("outputs"))
        {
            Directory.CreateDirectory("outputs");
        }
        var json = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync($"outputs/{path}", json);
    }
}

public class QueryService
{
    public async ValueTask<JsonNode> GetNode(string path, int index = 0)
    {
        string indexPath = index > 1 ? $"-{index}" : string.Empty;
        string fullPath = $"inputs/{path}{indexPath}.json";
        string strData = await File.ReadAllTextAsync(fullPath);

        var jsonNode = JsonNode.Parse(strData) ?? throw new Exception("Invalid JSON");
        return jsonNode;
    }

    public async ValueTask<IEnumerable<JsonNode>> GetNodes(string path)
    {
        JsonNode currentNode, page;
        int index = 0;
        LinkedList<JsonNode> nodes = new();
        do
        {
            currentNode = await GetNode(path, ++index);
            nodes.AddLast(currentNode);
            page = currentNode["page"]!;
        } while (page["currentPage"]!.GetValue<int>() != page["totalPages"]!.GetValue<int>());
        return nodes;
    }
}

public static class QueryServiceExtension
{
    private static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
    public static IEnumerable<T> Deserialize<T>(this IEnumerable<JsonNode> nodes, string nodeSelector)
        => nodes.SelectMany(currentNode =>
            JsonSerializer.Deserialize<IEnumerable<T>>(currentNode[nodeSelector], options)
                ?? throw new Exception("Unable to parse node")
        );
}

/*********************************************/
// DTO
/*********************************************/

#nullable disable
public class DataType
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class DataTypeOutput
{
    public string Name { get; set; }
    override public string ToString() => Name;
}

public class Metric
{
    public int Id { get; set; }
    public string Volume { get; set; }
}

public class MetricOutput
{
    public string Description { get; set; }
    override public string ToString() => Description;
}