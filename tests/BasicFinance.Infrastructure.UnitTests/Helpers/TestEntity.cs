namespace BasicFinance.Infrastructure.UnitTests.Helpers;

internal sealed class TestEntity
{
    public int Id { get; }
    public string Name { get; }

    public TestEntity(int id, string name)
    {
        Id = id;
        Name = name;
    }
}