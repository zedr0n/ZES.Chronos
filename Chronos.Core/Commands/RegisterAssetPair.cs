using Newtonsoft.Json;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Commands;

 /// <summary>
 /// Represents a command to register an asset pair in the system.
 /// </summary>
 /// <remarks>
 /// The command associates two assets — a "for" asset and a "dom" (dominant) asset —
 /// into an asset pair, with an identifier comprising the combination of these assets.
 /// </remarks>
 [method: JsonConstructor]
 public class RegisterAssetPair(string fordom, Asset forAsset, Asset domAsset, bool supportsIntraday = true) : Command, ICreateCommand
 {
     /// <summary>
     /// Initializes a new instance of the <see cref="RegisterAssetPair"/> class.
     /// Command to register an asset pair in the system.
     /// </summary>
     /// <param name="forAsset">Foreign asset in the pair</param>
     /// <param name="domAsset">Domestic asset in the pair</param>
     /// <remarks>
     /// This command creates an association between two assets, referred to as the "for" asset and
     /// the "dom" (dominant) asset. The combination of these assets creates a unique identifier
     /// for the asset pair, known as "fordom".
     /// </remarks>
     public RegisterAssetPair(Asset forAsset, Asset domAsset)
         : this(AssetPair.Fordom(forAsset, domAsset), forAsset, domAsset) { }
     
     /// <summary>
     /// Gets the combined identifier for the asset pair, which is typically
     /// represented as a concatenation of the foreign and domestic asset IDs.
     /// </summary>
     public string Fordom => fordom;

     /// <summary>
     /// Gets the primary asset in the asset pair being registered. Represents the "for" asset in the pair,
     /// indicating what the asset is being traded or evaluated against.
     /// </summary>
     public Asset ForAsset => forAsset;

     /// <summary>
     /// Gets the domestic asset associated with the asset pair.
     /// </summary>
     /// <remarks>
     /// The domestic asset represents the asset used as a reference or base
     /// in conjunction with the foreign asset in the context of an asset pair.
     /// </remarks>
     public Asset DomAsset => domAsset;

     /// <summary>
     /// Gets a value indicating whether indicates whether intraday trading or operations are supported for the asset pair.
     /// </summary>
     /// <remarks>
     /// This property determines if the asset pair allows actions or transactions to be performed
     /// within the same trading day. It is typically used to configure or limit functionality
     /// related to short-term financial operations.
     /// </remarks>
     public bool SupportsIntraday => supportsIntraday;
     
     /// <inheritdoc/>
     public override string Target => fordom;
 }