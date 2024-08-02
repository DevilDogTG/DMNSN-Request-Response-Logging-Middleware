using DMNSN.AspNetCore.Middlewares.RqRsLogging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text;

namespace DMNSN.Tests.Middlewares.RqRsLogging
{// File: tests/RqRsLoggingBaseTests.cs
    public class RqRsLoggingBaseTests
    {
        [Fact]
        public async Task ReadRequestBody_ShouldReturnRequestBodyAsString()
        {
            // Arrange
            var requestBody = "Test Request Body";
            var request = new DefaultHttpContext().Request;
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            var rqRsLoggingBase = new RqRsLoggingBase();

            // Act
            var result = await rqRsLoggingBase.ReadRequestBody(request);

            // Assert
            Assert.Equal(requestBody, result);
        }

        [Fact]
        public void TruncateLargeFields_ShouldTruncateFieldsExceedingMaxLength()
        {
            // Arrange
            var jsonString = "{\"field1\":\"value1\",\"field2\":\"This is a very long field value that should be truncated.\"}";
            var maxFieldLength = 10;
            var rqRsLoggingBase = new RqRsLoggingBase();

            // Act
            var result = rqRsLoggingBase.TruncateLargeFields(jsonString, maxFieldLength);

            // Assert
            Assert.Contains("This is a ...", result);
        }

        [Fact]
        public void TruncateText_ShouldTruncateTextExceedingMaxLength()
        {
            // Arrange
            var text = "This is a very long text.";
            var maxLength = 10;
            var rqRsLoggingBase = new RqRsLoggingBase();

            // Act
            var result = rqRsLoggingBase.TruncateText(text, maxLength);

            // Assert
            Assert.Equal("This is a ...", result);
        }

        [Fact]
        public void SerializeDictionary_ShouldReturnJsonString()
        {
            // Arrange
            var dictionary = new Dictionary<string, StringValues>
        {
            { "key1", new StringValues("value1") },
            { "key2", new StringValues("value2") }
        };
            var rqRsLoggingBase = new RqRsLoggingBase();

            // Act
            var result = rqRsLoggingBase.SerializeDictionary(dictionary);

            // Assert
            Assert.Contains("\"key1\":\"value1\"", result);
            Assert.Contains("\"key2\":\"value2\"", result);
        }
    }

}