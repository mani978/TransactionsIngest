namespace TransactionsIngest.App.Dtos;

public class IngestionResultDto
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Revoked { get; set; }
    public int Finalized { get; set; }
    public int NoChange { get; set; }
}