using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

static public class camcorder
{
    static public List<Tuple<Vector2, Vector3>> entries;
    static camcorder()
    {
        entries = new List<Tuple<Vector2, Vector3>>();
    }

    static public void WritePos()
    {
        XmlTextWriter writer = new XmlTextWriter("d:\\campath.xml", null);
        writer.Formatting = Formatting.Indented;
        writer.WriteStartDocument();

        writer.WriteStartElement("entries");
        foreach (var item in entries)
        {
            writer.WriteStartElement("entry");

            writer.WriteStartElement("ori");
            writer.WriteAttributeString("x", item.Item1.X.ToString());
            writer.WriteAttributeString("y", item.Item1.Y.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("pos");
            writer.WriteAttributeString("x", item.Item2.X.ToString());
            writer.WriteAttributeString("y", item.Item2.Y.ToString());
            writer.WriteAttributeString("z", item.Item2.Z.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteEndDocument();
        writer.Flush();
        writer.Close();
    }

    static public void WriteLights()
    {
        XmlTextWriter writer = new XmlTextWriter("d:\\lights.xml", null);
        writer.Formatting = Formatting.Indented;
        writer.WriteStartDocument();

        writer.WriteStartElement("lights");
        foreach (var item in DeferredLighting.DeferredRenderer.drinstance.spotLights)
        {
            writer.WriteStartElement("light");

            writer.WriteStartElement("pos");
            writer.WriteAttributeString("x", item.lightPosition.X.ToString());
            writer.WriteAttributeString("y", item.lightPosition.Y.ToString());
            writer.WriteAttributeString("z", item.lightPosition.Z.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("dir");
            writer.WriteAttributeString("x", item.lightDirection.X.ToString());
            writer.WriteAttributeString("y", item.lightDirection.Y.ToString());
            writer.WriteAttributeString("z", item.lightDirection.Z.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("col");
            writer.WriteAttributeString("r", item.lightColor.R.ToString());
            writer.WriteAttributeString("g", item.lightColor.G.ToString());
            writer.WriteAttributeString("b", item.lightColor.B.ToString());
            writer.WriteAttributeString("a", item.lightColor.A.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("other");
            writer.WriteAttributeString("intensity", item.lightIntensity.ToString());
            writer.WriteAttributeString("radius", item.lightRadius.ToString());
            writer.WriteAttributeString("angle", item.spotAngle.ToString());
            writer.WriteAttributeString("decay", item.decay.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();

        }
        writer.WriteEndElement();

        writer.WriteEndDocument();
        writer.Flush();
        writer.Close();
    }

    static public void Read()
    {
        XmlDocument document = new XmlDocument();

        try
        {
            document.Load("d:\\campath.xml");
            entries.Clear();
        }
        catch
        {
            return;
        }

        XmlNodeList rootElement = document.DocumentElement.GetElementsByTagName("entry");
        foreach (XmlNode entryelements in rootElement)
        {
            Vector2 ori = default(Vector2);
            Vector3 pos = default(Vector3);
            int n = 0;
            foreach (XmlNode entrysubelements in entryelements.ChildNodes)
            {
                if (entrysubelements.Name == "ori")
                {
                    float x = float.Parse(entrysubelements.Attributes["x"].Value);
                    float y = float.Parse(entrysubelements.Attributes["y"].Value);
                    ori = new Vector2(x, y);
                    n++;
                }
                else if (entrysubelements.Name == "pos")
                {
                    float x = float.Parse(entrysubelements.Attributes["x"].Value);
                    float y = float.Parse(entrysubelements.Attributes["y"].Value);
                    float z = float.Parse(entrysubelements.Attributes["z"].Value);
                    pos = new Vector3(x, y, z);
                    n++;
                }
            }
            if (n == 2)
            {
                entries.Add(new Tuple<Vector2, Vector3>(ori, pos));
            }
        }

        try
        {
            document.Load("d:\\lights.xml");
            DeferredLighting.DeferredRenderer.drinstance.spotLights.Clear();
        }
        catch
        {
            return;
        }

        rootElement = document.DocumentElement.GetElementsByTagName("light");
        foreach (XmlNode entryelements in rootElement)
        {
            Vector3 dir = default(Vector3);
            Vector3 pos = default(Vector3);
            Color col = default(Color);
            float intensity = 0;
            float radius = 0;
            float angle = 0;
            float decay = 0;

            foreach (XmlNode entrysubelements in entryelements.ChildNodes)
            {
                if (entrysubelements.Name == "dir")
                {
                    float x = float.Parse(entrysubelements.Attributes["x"].Value);
                    float y = float.Parse(entrysubelements.Attributes["y"].Value);
                    float z = float.Parse(entrysubelements.Attributes["z"].Value);
                    dir = new Vector3(x, y, z);
                }
                else if (entrysubelements.Name == "pos")
                {
                    float x = float.Parse(entrysubelements.Attributes["x"].Value);
                    float y = float.Parse(entrysubelements.Attributes["y"].Value);
                    float z = float.Parse(entrysubelements.Attributes["z"].Value);
                    pos = new Vector3(x, y, z);
                }
                else if (entrysubelements.Name == "col")
                {
                    float r = float.Parse(entrysubelements.Attributes["r"].Value);
                    float g = float.Parse(entrysubelements.Attributes["g"].Value);
                    float b = float.Parse(entrysubelements.Attributes["b"].Value);
                    float a = float.Parse(entrysubelements.Attributes["a"].Value);
                    col = new Color(r, g, b, a);
                }
                else if (entrysubelements.Name == "other")
                {
                    intensity = float.Parse(entrysubelements.Attributes["intensity"].Value);
                    radius = float.Parse(entrysubelements.Attributes["radius"].Value);
                    angle = float.Parse(entrysubelements.Attributes["angle"].Value);
                    decay = float.Parse(entrysubelements.Attributes["decay"].Value);
                }
            }
            DeferredLighting.DeferredRenderer.drinstance.spotLights.Add(new DeferredLighting.SpotLight()
            {
                decay = decay,
                lightIntensity = intensity,
                lightRadius = radius,
                spotAngle = angle,
                lightPosition = pos,
                lightDirection = dir,
                lightColor = col
            });
        }
    }
}
