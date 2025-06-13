namespace IksAdminApi;

public class DBResult
{
    // Применяется в функциях с запросами в базу данных
    public int? ElementId { get; set; }
    public int QueryStatus { get; set; }
    public string QueryMessage { get; set; }

    public DBResult(int? elementId, int queryStatus, string insertMessage = "OK!")
    {
        ElementId = elementId;
        QueryStatus = queryStatus;
        QueryMessage = insertMessage.ToLower();
        AdminUtils.LogDebug($"DB RESULT CREATED: \nelementId:{elementId}\nqueryStatus:{queryStatus}\ninsertMessage:{insertMessage}");
    }
}