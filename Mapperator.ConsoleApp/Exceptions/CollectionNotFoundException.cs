using System;
using Mapperator.ConsoleApp.Resources;

namespace Mapperator.ConsoleApp.Exceptions {
    public class CollectionNotFoundException : Exception {
        public CollectionNotFoundException(string collectionName) : base(string.Format(Strings.CouldNotFindCollection, collectionName)) { }
    }
}
