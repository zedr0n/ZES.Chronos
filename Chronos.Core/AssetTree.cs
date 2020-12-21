using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime;
using QuickGraph;
using QuickGraph.Algorithms;
using ZES.Interfaces;

namespace Chronos.Core
{
    public class AssetTree
    {
        private readonly BidirectionalGraph<string, RateEdge> _graph = new BidirectionalGraph<string, RateEdge>();
        
        public ILog Log { get; set; }
        
        public void Add(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset.Ticker))
                _graph.AddVertex(forAsset.Ticker);
            if (!_graph.ContainsVertex(domAsset.Ticker))
                _graph.AddVertex(domAsset.Ticker);
            
            var edge = new RateEdge(forAsset.Ticker, domAsset.Ticker);
            if (!_graph.ContainsEdge(edge))
                _graph.AddEdge(edge);
            
            var inverseEdge = new RateEdge(domAsset.Ticker, forAsset.Ticker);
            if (!_graph.ContainsEdge(inverseEdge))
                _graph.AddEdge(inverseEdge);
        }

        public IEnumerable<(string forAsset, string domAsset)> GetPath(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset.Ticker) || !_graph.ContainsVertex(domAsset.Ticker))
                return null;

            return _graph.RankedShortestPathHoffmanPavley(e => 1.0, forAsset.Ticker, domAsset.Ticker, 1).FirstOrDefault()?.Select(e => (e.Source, e.Target));
        }

        public string GetUrl(string forAsset, string domAsset, Instant date)
        {
            var edge = _graph.Edges.SingleOrDefault(e => e.Source == forAsset && e.Target == domAsset);
            return edge?.Url.Replace("$date", date.ToString("yyyy-mm-dd", new DateTimeFormatInfo()));
        }
        
        public void SetUrl(Asset forAsset, Asset domAsset, string url)
        {
            var edge = _graph.Edges.SingleOrDefault(e => e.Source == forAsset.Ticker && e.Target == domAsset.Ticker);
            if (edge == null)
                return;
            edge.Url = url;
        }

        private class RateEdge : Edge<string>
        {
            public RateEdge(string source, string target)
                : base(source, target)
            {
            }
            
            public string Url { get; set; }
        }
    }
}