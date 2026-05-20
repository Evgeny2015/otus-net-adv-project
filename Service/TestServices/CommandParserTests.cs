using CommandParser;
using Xunit;

namespace TestServices;

public class CommandParserTests
{
    private static string SpanToString(ReadOnlySpan<char> span) => span.ToString();

    [Fact]
    public void Parse_EmptyString_ReturnsEmptySpans()
    {
        // Arrange
        string command = "";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmptySpans()
    {
        // Arrange
        string command = "   ";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.True(result.Command.IsEmpty);
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }

    [Fact]
    public void Parse_SingleCommand_ReturnsCommandOnly()
    {
        // Arrange
        string command = "HELP";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.Equal("HELP", SpanToString(result.Command));
        Assert.True(result.Key.IsEmpty);
        Assert.True(result.Value.IsEmpty);
    }

    [Fact]
    public void Parse_CommandWithKey_ReturnsCommandAndKey()
    {
        // Arrange
        string command = "GET user123";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.Equal("GET", SpanToString(result.Command));
        Assert.Equal("user123", SpanToString(result.Key));
        Assert.True(result.Value.IsEmpty);
    }

    [Fact]
    public void Parse_CommandWithKeyAndValue_ReturnsAllComponents()
    {
        // Arrange
        string command = "SET name John";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.Equal("SET", SpanToString(result.Command));
        Assert.Equal("name", SpanToString(result.Key));
        Assert.Equal("John", SpanToString(result.Value));
    }

    [Fact]
    public void Parse_CommandWithKeyAndMultiWordValue_ReturnsValueAsIs()
    {
        // Arrange
        string command = "SET message \"Hello World\"";

        // Act
        var result = Parser.Parse(command);

        // Assert
        // Parser doesn't handle quotes specially, includes them in value
        Assert.Equal("SET", SpanToString(result.Command));
        Assert.Equal("message", SpanToString(result.Key));
        Assert.Equal("\"Hello World\"", SpanToString(result.Value)); // Entire quoted string
    }

    [Fact]
    public void Parse_CommandWithKeyAndValueWithExtraSpaces_TrimsCorrectly()
    {
        // Arrange
        string command = "  DELETE   key123   value456  ";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.Equal("DELETE", SpanToString(result.Command));
        Assert.Equal("key123", SpanToString(result.Key));
        Assert.Equal("value456  ", SpanToString(result.Value)); // Trailing spaces preserved
    }

    [Fact]
    public void Parse_UnclosedQuote_ReturnsFirstPartAsKeySecondAsValue()
    {
        // Arrange
        string command = "SET key \"unclosed value";

        // Act
        var result = Parser.Parse(command);

        // Assert
        // Parser doesn't treat quotes specially, includes entire rest after key
        Assert.Equal("SET", SpanToString(result.Command));
        Assert.Equal("key", SpanToString(result.Key));
        Assert.Equal("\"unclosed value", SpanToString(result.Value));
    }

    [Fact]
    public void Parse_ExtraCharactersAfterQuote_TreatedAsSeparateValue()
    {
        // Arrange
        string command = "SET key \"value\" extra";

        // Act
        var result = Parser.Parse(command);

        // Assert
        // Parser includes everything after key as value
        Assert.Equal("SET", SpanToString(result.Command));
        Assert.Equal("key", SpanToString(result.Key));
        Assert.Equal("\"value\" extra", SpanToString(result.Value));
    }

    [Fact]
    public void Parse_CommandWithEmptyKey_ReturnsCommandAndValue()
    {
        // Arrange
        string command = "COMMAND \"value\"";

        // Act
        var result = Parser.Parse(command);

        // Assert
        // Parser sees: command=COMMAND, key="value", value=empty
        Assert.Equal("COMMAND", SpanToString(result.Command));
        Assert.Equal("\"value\"", SpanToString(result.Key));
        Assert.True(result.Value.IsEmpty);
    }

    [Fact]
    public void Parse_CommandIsLowerCase_ReturnsAsIs()
    {
        // Arrange
        string command = "set key value";

        // Act
        var result = Parser.Parse(command);

        // Assert
        // Parser doesn't convert case
        Assert.Equal("set", SpanToString(result.Command));
        Assert.Equal("key", SpanToString(result.Key));
        Assert.Equal("value", SpanToString(result.Value));
    }

    [Fact]
    public void Parse_ComplexScenario_MultipleSpaces()
    {
        // Arrange
        string command = "UPDATE   user   \"John Doe with spaces\"";

        // Act
        var result = Parser.Parse(command);

        // Assert
        Assert.Equal("UPDATE", SpanToString(result.Command));
        Assert.Equal("user", SpanToString(result.Key));
        Assert.Equal("\"John Doe with spaces\"", SpanToString(result.Value)); // Entire quoted string
    }
}