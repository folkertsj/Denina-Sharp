﻿using System;
using DeninaSharp.Core;
using DeninaSharp.Core.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class XmlTests
    {
        [TestInitialize]
        public void Init()
        {
            Pipeline.Init();
            Pipeline.SetGlobalVariable(File.SANDBOX_VARIABLE_NAME, AppDomain.CurrentDomain.BaseDirectory);
        }

        [TestMethod]
        public void TransformXml()
        {
            // This test relies on controlled XSLT/XML files in the "Utility" folder
            var pipeline = GetPipeline();
            pipeline.AddCommand("File.Read -file:utility/data.xml => $xml");
            pipeline.AddCommand("File.Read -file:utility/transform.xslt => $xslt");
            pipeline.AddCommand("Xml.Transform -xslt:$xslt -xml:$xml");


            var result = pipeline.Execute();

            Assert.IsTrue(result.EndsWith("Deane"));
        }

        [TestMethod]
        public void FormatNodes()
        {
            var pipeline = GetPipeline();
            pipeline.AddCommand("File.Read -file:utility/data.xml");
            pipeline.AddCommand("Xml.FormatNodes -xpath://element -template:\"Value: {.}\"");
            var result = pipeline.Execute();

            Assert.AreEqual(result, "Value: Deane");
        }

        [TestMethod]
        public void CountNodes()
        {

            var pipeline = GetPipeline();
            pipeline.AddCommand("File.Read -file:utility/data.xml");
            pipeline.AddCommand("Xml.CountNodes -xpath://element");
            
            Assert.AreEqual(pipeline.Execute(), "1");
        }

        private Pipeline GetPipeline()
        {
            var pipeline = new Pipeline();
            return pipeline;
        }
       
    }
}
