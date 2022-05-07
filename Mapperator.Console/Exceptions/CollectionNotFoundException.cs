using System;
using Mapperator.Console.Resources;

namespace Mapperator.Console.Exceptions {
    public class CollectionNotFoundException : Exception {
        public CollectionNotFoundException(string collectionName) : base(string.Format(Strings.CouldNotFindCollection, collectionName)) { }
    }
}
