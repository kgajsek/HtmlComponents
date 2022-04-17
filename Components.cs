// https://github.com/kgajsek/HtmlComponents

namespace WebAPI {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public class NodeContent {
        public string ID { get; set; }
        public string Content { get; set; }
    }

    public class HtmlComponent {
        public string ID { get; set; }
        public string Tag { get; set; }
        public string NodeType { get; set; }
        public string LastHash { get; set; }
        public bool IsMain { get; set; }

        public static Func<string,string> TranslationFunction = null;
        
        private List<HtmlComponent> Components { get; set; }
        public void RegisterComponent (string tag, Func<HtmlComponent> component) {
            var existing = this.Components.FirstOrDefault (c => c.Tag == tag);
            if (existing == null) {
                var comp = component ();
                if (comp.Tag != tag) {
                    throw new ApplicationException ("RegisterComponent: Tag mismatch: " + comp.Tag + " vs. " + tag);
                }
                this.Components.Add (comp);
            }
        }
        public void UnregisterAllComponents () {
            var garbage = this.Components;
            this.Components = new List<HtmlComponent> ();
            garbage.ForEach (g => g.UnregisterAllComponents ());
        }
        public void UnregisterComponent (string tag) {
            var garbage = this.Components.FirstOrDefault (c => c.Tag == tag);
            this.Components = this.Components.Where (c => c.Tag != tag).ToList ();
            if (garbage != null) {
                garbage.UnregisterAllComponents ();
            }
        }

        public HtmlComponent () {
            this.ID = "c" + Guid.NewGuid().ToString ().Split ('-') [0];
            this.Components = new List<HtmlComponent> ();
            this.NodeType = "div";
            this.IsMain = false;
        }

        public virtual string Render () {
            return null;
        }

        public virtual string TagRender (bool dropTopDiv = false) {
            if (dropTopDiv || IsMain || string.IsNullOrWhiteSpace (this.NodeType)) {
                return Translate (this.Render ());
            } else {
                return "<" + this.NodeType + " id='" + this.ID + "'>" + Translate (this.Render ()) + "</" + this.NodeType + ">";
            }
        }

        private string Translate (string html) {
            if ((TranslationFunction != null) && !string.IsNullOrWhiteSpace (html)) {
                var tagPrefix = "{{T:";
                while (html.Contains (tagPrefix)) {
                    var startPos = html.IndexOf (tagPrefix) + tagPrefix.Length;
                    var endPos = html.Substring (startPos).IndexOf ("}}");
                    var tag = html.Substring (startPos, endPos);
                    var translation = TranslationFunction (tag);
                    html = html.Replace ("{{T:" + tag + "}}", translation);
                }
            }
            return html;
        }

        private bool HasChanged () {
            return this.LastHash != this.TagRender ();
        }

        private string SubRender (string baseHtml) {
            var finalHtml = baseHtml;
            this.Components.ForEach (c => {
                if (finalHtml.Contains ("{{" + c.Tag + "}}")) {
                    var r = c.TagRender ();
                    c.LastHash = r;
                    finalHtml = finalHtml.Replace ("{{" + c.Tag + "}}", r);
                    finalHtml = c.SubRender (finalHtml);
                } else {
                    c.LastHash = "";
                }
            });            
            return finalHtml;
        }

        private void SubScan (List<NodeContent> ncs) {
            this.Components.ForEach (c => {
                if (c.HasChanged ()) {
                    ncs.Add (new NodeContent { ID = c.ID, Content = c.FullHtml () });
                } else {
                    c.SubScan (ncs);
                }
            });
        }

        public string FullHtml (bool dropTopDiv = false) {
            var baseHtml = TagRender (dropTopDiv);
            this.LastHash = baseHtml;
            var finalHtml = SubRender (baseHtml);
            return finalHtml;
        }

        public List<NodeContent> DiffHtml () {
            if (!this.IsMain) { throw new ApplicationException ("Diff render only possible on main component!"); }
            var ncs = new List<NodeContent> ();
            if (this.HasChanged ()) {
                ncs.Add (new NodeContent { ID = "main", Content = this.FullHtml (true) });
            } else {
                SubScan (ncs);
            }
            return ncs;
        }
        
        public static string Enc (string html) {
            return HttpUtility.HtmlEncode (html);
        }

        public HtmlComponent Find (string tag) {
            var comp = this.Components.FirstOrDefault (c => c.Tag == tag);
            if (comp != null) { return comp; }
            foreach (var c in this.Components) {
                var sc = c.Find (tag);
                if (sc != null) { return sc; }
            }
            return null;
        }
    }
}