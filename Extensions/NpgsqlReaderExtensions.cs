using Npgsql;
using System.Data;
using TaskManagementApi.Models;


namespace TaskManagementApi.Extensions;

public static class NpgsqlReaderExtensions
{
    public static TaskItem ReadTaskItem(this NpgsqlDataReader reader)
        => new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2), reader.GetDateTime(3), reader.GetDateTime(4));
}