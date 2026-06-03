namespace DORM.CLI.Models;

public record TableStatistic(string TableName, long SizeBytes, long DataLength, long IndexLength);