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
 public class RegisterAssetPair(string fordom, Asset forAsset, Asset domAsset) : Command, ICreateCommand
 {
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

     /// <inheritdoc/>
     public override string Target => fordom;
 }