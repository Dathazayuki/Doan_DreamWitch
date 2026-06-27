using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Player
{
    [CreateAssetMenu(fileName = "SpellBookDatabase", menuName = "DreamKnight/Items/Spell Book Database")]
    public class SpellBookDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<SpellBookSO> spellBooks = new List<SpellBookSO>();

        public IReadOnlyList<SpellBookSO> SpellBooks => spellBooks;

        public SpellBookSO FindById(string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId))
                return null;

            for (int i = 0; i < spellBooks.Count; i++)
            {
                SpellBookSO book = spellBooks[i];
                if (book != null && book.bookId == bookId)
                    return book;
            }

            return null;
        }
    }
}
