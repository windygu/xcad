﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xarial.XCad.SolidWorks.Documents;
using Xarial.XCad.SolidWorks.Documents.Exceptions;

namespace SolidWorks.Tests.Integration
{
    public class ComponentsTest : IntegrationTests
    {
        [Test]
        public void IterateRootComponentsTest() 
        {
            string[] compNames;

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                compNames = ((ISwAssembly)m_App.Documents.Active).Components.Select(c => c.Name).ToArray();
            }

            Assert.That(compNames.OrderBy(c => c).SequenceEqual(
                new string[] { "Part1-1", "Part1-2", "SubAssem1-1", "SubAssem1-2", "SubAssem2-1", "Part1-3" }.OrderBy(c => c)));
        }

        [Test]
        public void IterateSubComponentsTest()
        {
            string[] compNames;

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                var assm = (ISwAssembly)m_App.Documents.Active;
                var comp = assm.Components["SubAssem1-1"];
                compNames = comp.Children.Select(c => c.Name).ToArray();
            }

            Assert.That(compNames.OrderBy(c => c).SequenceEqual(
                new string[] { "SubAssem1-1/Part2-1", "SubAssem1-1/SubSubAssem1-1" }.OrderBy(c => c)));
        }

        [Test]
        public void GetDocumentTest() 
        {
            bool doc1Contains;
            bool doc2Contains;
            string doc1FileName;
            string doc2FileName;

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                var assm = (ISwAssembly)m_App.Documents.Active;

                var doc1 = assm.Components["Part1-1"].Document;
                doc1FileName = Path.GetFileName(doc1.Path);
                doc1Contains = m_App.Documents.Contains(doc1);

                var doc2 = assm.Components["SubAssem1-1"].Document;
                doc2FileName = Path.GetFileName(doc2.Path);
                doc2Contains = m_App.Documents.Contains(doc2);

                Assert.Throws<ComponentNotLoadedException>(() => { var d = assm.Components["Part1-2"].Document; });
            }

            Assert.That(doc1FileName.Equals("Part1.sldprt", StringComparison.CurrentCultureIgnoreCase));
            Assert.That(doc2FileName.Equals("SubAssem1.sldasm", StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(doc1Contains);
            Assert.IsTrue(doc2Contains);
        }

        [Test]
        public void IterateFeaturesTest()
        {
            var featNames = new List<string>();

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                var comp = ((ISwAssembly)m_App.Documents.Active).Components["Part1-1"];

                foreach (var feat in comp.Features)
                {
                    featNames.Add(feat.Name);
                }
            }
            
            Assert.That(featNames.SequenceEqual(
                new string[] { "Favorites", "Selection Sets", "Sensors", "Design Binder", "Annotations", "Notes", "Notes1___EndTag___", "Surface Bodies", "Solid Bodies", "Lights, Cameras and Scene", "Ambient", "Directional1", "Directional2", "Directional3", "Markups", "Equations", "Material <not specified>", "Front Plane", "Top Plane", "Right Plane", "Origin", "Sketch1", "Boss-Extrude1" }));
        }
    }
}
