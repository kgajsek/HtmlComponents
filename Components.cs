// https://github.com/kgajsek/HtmlComponents

namespace WebAPI {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class NodeContent {
        public string ID { get; set; }
        public string Content { get; set; }
    }

    public class HtmlComponents {
        private Dictionary<string, HtmlComponent> dict = new Dictionary<string, HtmlComponent> ();

        public void Add (string tag, HtmlComponent c) {
            dict [tag] = c;
        }

        public HtmlComponent Get (string key) {
            return dict [key];
        }

        public List<string> Keys {
            get {
                var l = new List<string> (dict.Count);
                foreach (string k in dict.Keys) {
                    l.Add (k);
                }
                return l;
            }            
        }
    }

    public class HtmlComponent {
        public string ID { get; set; }
        public string Tag { get; set; }
        public string NodeType { get; set; }
        public string LastHash { get; set; }
        public bool IsMain { get; set; }
        
        private List<HtmlComponent> Components { get; set; }
        public void RegisterComponent (string tag, Func<HtmlComponent> component) {
            var existing = this.Components.FirstOrDefault (c => c.Tag == tag);
            if (existing == null) {
                this.Components.Add (component ());
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

        public virtual string TagRender () {
            if (IsMain || string.IsNullOrWhiteSpace (this.NodeType)) {
                return this.Render ();
            } else {
                return "<" + this.NodeType + " id='" + this.ID + "'>" + this.Render () + "</" + this.NodeType + ">";
            }
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

        public string FullHtml () {
            var baseHtml = TagRender ();
            this.LastHash = baseHtml;
            var finalHtml = SubRender (baseHtml);
            return finalHtml;
        }

        public List<NodeContent> DiffHtml () {
            var ncs = new List<NodeContent> ();
            if (this.HasChanged ()) {
                ncs.Add (new NodeContent { ID = this.ID, Content = this.FullHtml () });
                return ncs;
            } else {
                SubScan (ncs);
                return ncs;
            }
        }
    }
}