using Npgsql;
using TaskManagementApi.Models;


namespace TaskManagementApi.Extensions;

public static class NpgsqlReaderExtensions
{
    public static TaskItem ReadTaskItem(this NpgsqlDataReader reader)
        => new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2));
}