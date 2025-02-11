using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReflexionJson.VersionTwo;

/*********************************************/
// Entry point 
/*********************************************/

public class Main
{
    public static async ValueTask Run()
    {
        QueryService service = new();
        TypeRepository typeRepository = new(service);

        var types = await typeRepository.GetTypes();

        foreach (var type in types)
        {
            Console.WriteLine($"Id: {type.Id}, Name: {type.Name}");
        }
    }
}

/*********************************************/
// Specific code 
/*********************************************/

class TypeRepository(QueryService service)
{
    public async ValueTask<IEnumerable<DataType>> GetTypes()
    {
        return await service.GetList<DataType>("inputs/types");
    }
}

/*********************************************/
// Generic code 
/*********************************************/


public class QueryService
{
    private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
    
    // private 
    public async ValueTask<JsonNode> GetNode(string path, int index = 0){
        string indexPath = index > 1 ? $"-{index}" : string.Empty;
        string fullPath = $"{path}{indexPath}.json";
        string strData = await File.ReadAllTextAsync(fullPath);

        var jsonNode = JsonNode.Parse(strData) ?? throw new Exception("Invalid JSON");
        return jsonNode;
    }
    
    // Quel est le problème de cette méthode ? 
    // Comment l'améliorer ? 
    public async ValueTask<IEnumerable<T>> GetList<T>(string path, string targetProperty = "data")
    {
        JsonNode page,currentNode;
        IEnumerable<T> fullOutput = new LinkedList<T>();
        int index = 0;
        do{
            currentNode = await GetNode(path, ++index);
            var output =
                JsonSerializer.Deserialize<IEnumerable<T>>(currentNode[targetProperty], options)
                ?? throw new Exception($"Invalid JSON format for {typeof(T).Name}");
            fullOutput = fullOutput.Concat(output);
            page = currentNode["page"]!;


        }while(page["currentPage"]!.GetValue<int>() != page["totalPages"]!.GetValue<int>());

        return fullOutput;
    }
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

public class Metric
{
    public int Id { get; set; }
    public string Description { get; set; }
}