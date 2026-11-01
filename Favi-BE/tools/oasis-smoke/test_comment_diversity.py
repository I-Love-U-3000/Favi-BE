import unittest

from run_autonomous import comment_diversity


class CommentDiversityTests(unittest.TestCase):
    def test_rejects_case_punctuation_duplicates(self):
        self.assertFalse(comment_diversity([(0, "Great food!"), (1, "GREAT food.")])["passed"])

    def test_rejects_near_duplicates(self):
        self.assertFalse(comment_diversity([(0, "What is the price of a meal at this market?"),
                                           (1, "What is the price of a meal in this market?")])["passed"])

    def test_requires_multiple_authors_and_comments(self):
        self.assertFalse(comment_diversity([])["passed"])
        self.assertFalse(comment_diversity([(0, "Bus fare?"), (0, "Is photography permitted?")])["passed"])

    def test_accepts_distinct_perspectives(self):
        self.assertTrue(comment_diversity([(0, "How much does the bus cost?"),
                                          (1, "Which spices go into the stew?")])["passed"])
