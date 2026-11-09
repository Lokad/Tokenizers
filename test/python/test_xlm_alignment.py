"""Origin goldens before segmentation, so unknown fusion cannot hide errors."""
import random
import unittest
import unicodedata

from xlm_reference import normalize_origins, prepare


class AlignmentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if unicodedata.unidata_version != "15.1.0":
            raise RuntimeError("Alignment qualification requires Python 3.13 / Unicode 15.1")

    def test_exact_origin_goldens(self):
        cases = [
            ("q\u0f72\u0f75", "q\u0f71\u0f72\u0f74", [[0],[2],[1],[2]]),
            ("q\u0f74\u0f73", "q\u0f71\u0f72\u0f74", [[0],[2],[2],[1]]),
            ("a\u0301\u0327\u0301", "á\u0327\u0301", [[0,1],[2],[3]]),
            ("e\u0301\u0301", "é\u0301", [[0,1],[2]]),
            ("ﬁ\u0301", "fí", [[0],[0,1]]),
            ("각", "각", [[0,1,2]]),
            ("🧪\0e\u0301", "🧪é", [[0],[2,3]]),
        ]
        for text,expected,origins in cases:
            with self.subTest(text=repr(text)):
                actual,mapping = normalize_origins(text,8,False)
                self.assertEqual(expected,actual)
                self.assertEqual([{p+8 for p in group} for group in origins],mapping)
        self.assertEqual(("i\u0307",[{0},{0}]), normalize_origins("İ",0,True))

    def test_cleanup_insertion_changes_positions_without_inventing_origins(self):
        rng = random.Random(20261109)
        alphabet = ["q","a","é","ﬁ","İ","ᄀ","ᅡ","ᆨ","\u0f72","\u0f75","\u0f73","\u0301","\u0327","🧪"," "]
        for _ in range(1000):
            text = "".join(rng.choice(alphabet) for _ in range(rng.randrange(25)))
            position = rng.randrange(len(text)+1)
            for lowercase in (False,True):
                normalized,mapping = normalize_origins(text,0,lowercase)
                self.assertEqual(prepare(text,lowercase),normalized)
                inserted,shifted = normalize_origins(text[:position]+"\0"+text[position:],0,lowercase)
                self.assertEqual(normalized,inserted)
                self.assertEqual([{p+(p>=position) for p in origins} for origins in mapping],shifted)
                self.assertTrue(all(origins for origins in mapping))


if __name__ == "__main__":
    unittest.main()
