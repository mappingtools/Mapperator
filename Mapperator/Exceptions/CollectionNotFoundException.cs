using Mapperator.Resources;
using System;

namespace Mapperator.Exceptions {
    public class CollectionNotFoundException : Exception {
        public CollectionNotFoundException(string collectionName) : base(string.Format(Strings.CouldNotFindCollection, collectionName)) { }
    }
}
