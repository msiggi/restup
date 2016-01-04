﻿using Devkoes.Restup.WebServer.Http.RequestParsers;
using Devkoes.Restup.WebServer.Models.Schemas;
using Devkoes.Restup.WebServer.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Devkoes.Restup.WebServer.UnitTests.Http.RequestParsers
{
    [TestClass]
    public class HttpRequestParserTest
    {
        //TODO uri relative/absolute test

        [TestMethod]
        public void ParseRequestStream_AllDataAtOnce_CompleteRequest()
        {
            var s = new HttpRequestParser();

            var streamedRequest = "GET /api/data HTTP/1.1\r\nContent-Length: 4\r\n\r\ndata";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(streamedRequest));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(HttpMethod.GET, request.Method);
            Assert.AreEqual(new Uri("/api/data", UriKind.Relative), request.Uri);
            Assert.AreEqual("data", request.Content);
            Assert.AreEqual(4, request.ContentLength);
            Assert.AreEqual("HTTP/1.1", request.HttpVersion);
            Assert.AreEqual(true, request.IsComplete);
        }


        [TestMethod]
        public void ParseRequestStream_AllHeaderTypes_AllHeadersParsed()
        {
            var s = new HttpRequestParser();

            var streamedRequest =
                @"GET /api/data HTTP/1.1
Content-Length: 4
Accept: application/json,text/xml
Accept-Charset: utf-7;q=0.2, utf-8;q=0.1,*;q=0
Content-Type: text/xml;charset=utf-8
UnknownHeader: some:value

data";

            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(streamedRequest));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
            Assert.IsTrue(request.Headers.Any(h => h.Name == "UnknownHeader" && h.RawContent == "some:value"));
            Assert.AreEqual(4, request.ContentLength);
            Assert.AreEqual(Encoding.UTF8, request.RequestContentEncoding);
            Assert.AreEqual(MediaType.XML, request.RequestContentType);
            Assert.AreEqual(Encoding.UTF7, request.ResponseContentEncoding);
            Assert.AreEqual(MediaType.JSON, request.ResponseContentType);
        }


        [TestMethod]
        public void ParseRequestStream_ContentLengthNumberMissing_RequestIncomplete()
        {
            var s = new HttpRequestParser();

            var streamedRequest =
                @"GET /api/data HTTP/1.1
Content-Length: four

data";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(streamedRequest));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(false, request.IsComplete);
        }

        [TestMethod]
        public void ParseRequestStream_TooMuchData_RequestIncomplete()
        {
            var s = new HttpRequestParser();

            var streamedRequest =
                @"GET /api/data HTTP/1.1
Content-Length: 4

data";
            var extraData = "plusanotherextrafewbytes";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(streamedRequest + extraData));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(false, request.IsComplete);
        }

        [TestMethod]
        public void ParseRequestStream_DataOverflowSecondStream_ValidRequest()
        {
            var s = new HttpRequestParser();

            var streamedRequest =
                @"GET /api/data HTTP/1.1
Content-Length: 4

data";
            var extraData = "plusanotherextrafewbytes";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(streamedRequest));
            byteStreamParts.Add(Encoding.UTF8.GetBytes(extraData));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
        }

        [TestMethod]
        public void ParseRequestStream_PartedData_ValidRequest()
        {
            var s = new HttpRequestParser();

            var httpHeadersPart1 =
                @"GET /api/data HTTP/1.1
Content-Length: 4

";
            var content = "data";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(httpHeadersPart1));
            byteStreamParts.Add(Encoding.UTF8.GetBytes(content));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
            Assert.AreEqual(content, request.Content);
        }

        [TestMethod]
        public void ParseRequestStream_PartedDataWithEmptyReponseInBetween_ValidRequest()
        {
            var s = new HttpRequestParser();

            var httpHeadersPart1 =
                @"GET /api/data HTTP/1.1
Content-Length: 4

";
            var content = "data";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(httpHeadersPart1));
            byteStreamParts.Add(new byte[] { });
            byteStreamParts.Add(Encoding.UTF8.GetBytes(content));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
            Assert.AreEqual(content, request.Content);
        }

        [TestMethod]
        public void ParseRequestStream_DataLengthInSecondPart_ValidRequest()
        {
            var s = new HttpRequestParser();

            var httpHeadersPart1 = "GET /api/data HTTP/1.1\r\n";
            var httpHeadersPart2 = "Content-Length: 4\r\n\r\n";
            var content = "data";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(httpHeadersPart1));
            byteStreamParts.Add(Encoding.UTF8.GetBytes(httpHeadersPart2));
            byteStreamParts.Add(new byte[] { });
            byteStreamParts.Add(Encoding.UTF8.GetBytes(content));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
            Assert.AreEqual(content, request.Content);
        }

        [TestMethod]
        public void ParseRequestStream_FragmentedData_ValidRequest()
        {
            var s = new HttpRequestParser();

            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes("GET /api/data HTTP/1.1\r\n"));
            byteStreamParts.Add(Encoding.UTF8.GetBytes("Content-Leng"));
            byteStreamParts.Add(Encoding.UTF8.GetBytes("th: 4\r\n"));
            byteStreamParts.Add(Encoding.UTF8.GetBytes("\r\nd"));
            byteStreamParts.Add(Encoding.UTF8.GetBytes("a"));
            byteStreamParts.Add(Encoding.UTF8.GetBytes("t"));
            byteStreamParts.Add(Encoding.UTF8.GetBytes("a"));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
            Assert.AreEqual("data", request.Content);
        }

        [TestMethod]
        public void ParseRequestStream_WithoutDataAndHeaders_CompleteRequest()
        {
            var s = new HttpRequestParser();

            var httpHeadersPart1 =
                @"GET /api/data HTTP/1.1

";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(httpHeadersPart1));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(true, request.IsComplete);
        }

        [TestMethod]
        public void ParseRequestStream_ThreeEmptyResponses_EmptyRequestString()
        {
            var s = new HttpRequestParser();

            var httpHeadersPart1 =
                @"GET /api/data HTTP/1.1
Content-Length: 4

";
            var body = "data";
            var byteStreamParts = new List<byte[]>();
            byteStreamParts.Add(Encoding.UTF8.GetBytes(httpHeadersPart1));
            byteStreamParts.Add(new byte[] { });
            byteStreamParts.Add(new byte[] { });
            byteStreamParts.Add(new byte[] { });
            byteStreamParts.Add(new byte[] { });
            byteStreamParts.Add(Encoding.UTF8.GetBytes(body));

            var request = s.ParseRequestStream(new TestStream(byteStreamParts)).Result;

            Assert.AreEqual(false, request.IsComplete);
        }
    }
}