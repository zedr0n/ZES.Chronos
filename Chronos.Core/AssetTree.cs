using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Core
{
    /// <summary>
    /// Asset tree graph
    /// </summary>
    public class AssetTree
    {
        private readonly BidirectionalGraph<Asset, RateEdge> _graph = new BidirectionalGraph<Asset, RateEdge>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetTree"/> class.
        /// </summary>
        public AssetTree()
        {
            _graph.AddVertex(new Currency("GBP"));
            _graph.AddVertex(new Currency("USD"));
        }
        
        /// <summary>
        /// Gets or sets the log service
        /// </summary>
        public ILog Log { get; set; }

        /// <summary>
        /// Gets the registered assets
        /// </summary>
        public IEnumerable<Asset> Assets => _graph.Vertices;
        
        /// <summary>
        /// Register the FORDOM pair
        /// </summary>
        /// <param name="forAsset">Foreign asset</param>
        /// <param name="domAsset">Domestic asset</param>
        public void Add(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset))
                _graph.AddVertex(forAsset);
            if (!_graph.ContainsVertex(domAsset))
                _graph.AddVertex(domAsset);
            
            var edge = new RateEdge(forAsset, domAsset);
            if (!_graph.ContainsEdge(edge))
                _graph.AddEdge(edge);
            
            var inverseEdge = new RateEdge(domAsset, forAsset);
            if (!_graph.ContainsEdge(inverseEdge))
                _graph.AddEdge(inverseEdge);
        }

        /// <summary>
        /// Gets the pricing path from FOR to DOM
        /// </summary>
        /// <param name="forAsset">Foreign asset</param>
        /// <param name="domAsset">Domestic asset</param>
        /// <returns>Path enumerable</returns>
        public IEnumerable<(string ForAsset, string DomAsset)> GetPath(Asset forAsset, Asset domAsset)
        {
            if (!_graph.ContainsVertex(forAsset) || !_graph.ContainsVertex(domAsset))
                return null;

            return _graph.RankedShortestPathHoffmanPavley(e => 1.0, forAsset, domAsset, 1).FirstOrDefault()?.Select(e => (e.Source.Ticker, e.Target.Ticker));
        }

        private class RateEdge : Edge<Asset>
        {
            public RateEdge(Asset source, Asset target)
                : base(source, target)
            {
            }
            
            public string Url { get; set; }
        }
    }
}