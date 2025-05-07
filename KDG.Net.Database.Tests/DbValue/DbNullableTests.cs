using KDG.Database.Common;
using Npgsql;
using Moq;
using KDG.Database.Interfaces;
using KDG.Common;

namespace KDG.Database.Tests.DbValue;


public class DbNullableTests
{
    [Fact]
    public void HandleWrite_WithNullValue_WritesNull()
    {
        // Arrange
        var mockWriter = new Mock<IBulkWriter>();

        int? value = null;
        var nullableValue = new DbNullable<int>(value.ToOption(), (i) => new DbNumeric(i));

        // Act
        nullableValue.HandleWrite(mockWriter.Object);

        // Verify
        mockWriter.Verify(i => i.WriteNull(), Times.Once);
    }

    [Fact]
    public void HandleWrite_WithValue_WritesValue()
    {
        // Arrange
        var mockWriter = new Mock<IBulkWriter>();
        int? value = 42;
        var nullableValue = new DbNullable<int>(value.ToOption(), (i) => new DbNumeric(i));

        // Act
        nullableValue.HandleWrite(mockWriter.Object);

        // Verify
        mockWriter.Verify(i => i.WriteNull(), Times.Never);
        mockWriter.Verify(i => i.Write((decimal)value, NpgsqlTypes.NpgsqlDbType.Numeric), Times.Once);
    }

    [Fact]
    public void HandleWrite_WithNullReference_WritesNull()
    {
        // Arrange
        var mockWriter = new Mock<IBulkWriter>();

        string? text = null;
        var nullableString = new DbNullable<string>(text.ToOption(), s => new DbString(s));

        // Act
        nullableString.HandleWrite(mockWriter.Object);

        // Verify
        mockWriter.Verify(i => i.WriteNull(), Times.Once);
    }

    [Fact]
    public void HandleWrite_WithReference_WritesValue()
    {
        // Arrange
        var mockWriter = new Mock<IBulkWriter>();
        string? value = "Hello, World!";
        var nullableString = new DbNullable<string>(value.ToOption(), s => new DbString(s));

        // Act
        nullableString.HandleWrite(mockWriter.Object);

        // Verify
        mockWriter.Verify(i => i.WriteNull(), Times.Never);
        mockWriter.Verify(i => i.Write(value, NpgsqlTypes.NpgsqlDbType.Text), Times.Once);
    }

    [Fact]
    public void AddParameter_WithValue_AddsValueParameter()
    {
        // Arrange
        var mockBuilder = new Mock<IQueryBuilder>();
        int? value = 42;
        var nullableValue = new DbNullable<int>(value.ToOption(), (i) => new DbNumeric(i));
        var expectedParam = new NpgsqlParameter();
        mockBuilder.Setup(b => b.AddParameter("param", (decimal)value, NpgsqlTypes.NpgsqlDbType.Numeric)).Returns(expectedParam);

        // Act
        var result = nullableValue.AddParameter("param", mockBuilder.Object);

        // Verify
        Assert.Same(expectedParam, result);
        mockBuilder.Verify(b => b.AddParameter("param", (decimal)value, NpgsqlTypes.NpgsqlDbType.Numeric), Times.Once);
        mockBuilder.Verify(b => b.AddNull("param"), Times.Never);
    }

    [Fact]
    public void AddParameter_WithNullReference_AddsNullParameter()
    {
        // Arrange
        var mockBuilder = new Mock<IQueryBuilder>();
        string? text = null;
        var nullableString = new DbNullable<string>(text.ToOption(), s => new DbString(s));
        var expectedParam = new NpgsqlParameter();
        mockBuilder.Setup(b => b.AddNull("param")).Returns(expectedParam);

        // Act
        var result = nullableString.AddParameter("param", mockBuilder.Object);

        // Verify
        Assert.Same(expectedParam, result);
        mockBuilder.Verify(b => b.AddNull("param"), Times.Once);
    }

    public string? TestValue() => "Hello, World!";

    [Fact]
    public void AddParameter_WithReference_AddsValueParameter()
    {
        // Arrange
        var mockBuilder = new Mock<IQueryBuilder>();
        string? value = "Hello, World!";
        var nullableString = new DbNullable<string>(value.ToOption(), s => new DbString(s));
        var expectedParam = new NpgsqlParameter();
        mockBuilder.Setup(b => b.AddParameter("param", value, NpgsqlTypes.NpgsqlDbType.Text)).Returns(expectedParam);

        // Act
        var result = nullableString.AddParameter("param", mockBuilder.Object);

        // Verify
        Assert.Same(expectedParam, result);
        mockBuilder.Verify(b => b.AddParameter("param", value, NpgsqlTypes.NpgsqlDbType.Text), Times.Once);
        mockBuilder.Verify(b => b.AddNull("param"), Times.Never);
    }
}
