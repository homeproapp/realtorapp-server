using System;

namespace RealtorApp.Domain.Comparers;

public class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
  {
      public bool Equals(HashSet<T>? x, HashSet<T>? y)
      {
          if (x == null && y == null) return true;
          if (x == null || y == null) return false;

          return x.SetEquals(y);
      }

      public int GetHashCode(HashSet<T> obj)
      {
          if (obj == null) return 0;

          // XOR all hash codes for order-independent hashing
          int hash = 0;
          foreach (var item in obj)
          {
              hash ^= item?.GetHashCode() ?? 0;
          }
          return hash;
      }
  }
