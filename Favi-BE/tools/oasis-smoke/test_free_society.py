import unittest
from free_society import validate_turn, validate_profile, bounded_context, decision_schema, apply_event, PROFILE_SCHEMA


class FreeSocietyTests(unittest.TestCase):
    def turn(self, **kwargs):
        return dict(persona="I am undecided about my identity", emotion="bored and hopeful",
                    goal="meet someone", personal_event="I changed my mind", action="wait",
                    target=None, content="", sleep_ticks=3, **kwargs)

    def test_wait_and_self_selected_state_are_valid(self):
        turn = self.turn()
        self.assertEqual(validate_turn(turn, 0, {0, 1}, {2}, set(), set()), turn)

    def test_no_topic_or_profession_is_imposed(self):
        profile = dict(name="Someone", persona="A confused fictional poet", public_bio="Still thinking",
                       emotion="irritated", goal="do nothing today")
        self.assertEqual(validate_profile(profile), profile)

    def test_other_agents_cannot_be_invented(self):
        turn = self.turn()
        turn.update(action="follow", target=999)
        with self.assertRaises(ValueError):
            validate_turn(turn, 0, {0, 1}, {2}, set(), set())

    def test_only_observed_post_ids_can_be_targeted(self):
        turn = self.turn()
        turn.update(action="comment", target=99, content="hello")
        with self.assertRaises(ValueError):
            validate_turn(turn, 0, {0, 1}, {2}, set(), set())

    def test_reversible_relationships_and_invalid_self_follow(self):
        turn = self.turn()
        turn.update(action="unfollow", target=1)
        validate_turn(turn, 0, {0, 1}, {2}, set(), {1})
        turn.update(action="follow", target=0)
        with self.assertRaises(ValueError):
            validate_turn(turn, 0, {0, 1}, {2}, set(), set())

    def test_invalid_target_does_not_mutate_state(self):
        turn = self.turn()
        turn.update(action="like", target=True)
        original = dict(turn)
        with self.assertRaises(ValueError):
            validate_turn(turn, 0, {0, 1}, {1}, set(), set())
        self.assertEqual(turn, original)

    def test_event_membership_requires_an_actual_event(self):
        turn = self.turn()
        turn.update(action="join_event", target=1)
        with self.assertRaises(ValueError):
            validate_turn(turn, 0, {0, 1}, set(), set(), set())
        validate_turn(turn, 0, {0, 1}, set(), set(), set(), {1}, set())
        with self.assertRaises(ValueError):
            validate_turn(turn, 0, {0, 1}, set(), set(), set(), {1}, {1})

    def test_observation_trimming_never_changes_private_or_public_state(self):
        context = {"your_private_state": {"persona": "my choice", "memory": ["a" * 8000]},
                   "public_events": [{"id": 1, "members": [0, 1]}]}
        result = bounded_context(context)
        self.assertEqual(len(context["your_private_state"]["memory"]), 1)
        self.assertEqual(result["your_private_state"]["memory"], [])
        result["public_events"][0]["members"].remove(1)
        self.assertEqual(context["public_events"][0]["members"], [0, 1])

    def test_action_schema_offers_choices_without_activity_quota(self):
        schema = decision_schema(0, {0, 1, 2}, {10}, {10}, {1}, {3}, set())
        choices = {b["properties"]["action"]["const"]: b["properties"]["target"] for b in schema["anyOf"]}
        self.assertIn("wait", choices)
        self.assertNotIn("like", choices)
        self.assertEqual(choices["follow"]["enum"], [2])
        self.assertEqual(choices["unlike"]["enum"], [10])
        self.assertEqual(choices["post"], {"type": "null"})

    def test_schema_does_not_force_text_to_end_mid_word(self):
        self.assertTrue(all("maxLength" not in field for field in PROFILE_SCHEMA["properties"].values()))
        profile = dict(name="Person", persona="A human with a long but complete biography. " * 10,
                       public_bio="Hello", emotion="uncertain", goal="think")
        validate_profile(profile)
        profile["persona"] = "x" * 4097
        with self.assertRaises(ValueError):
            validate_profile(profile)

    def test_ordinary_long_emotion_is_not_a_failed_identity(self):
        profile = dict(name="Ava", persona="A person", public_bio="Hello", goal="think",
            emotion="A quiet thrill mixed with mild fatigue after a long coding session. The city feels alive but my body is tired from late work.")
        self.assertEqual(validate_profile(profile)["emotion"], profile["emotion"])

    def test_event_claim_does_not_enroll_other_people_or_even_its_owner(self):
        events, owner_joined, visitor_joined = [], [], []
        apply_event("event", 0, None, "Everyone will come", 123, 0, events, owner_joined)
        self.assertEqual(events[0]["members"], [])
        self.assertEqual(owner_joined, [])
        apply_event("join_event", 1, 1, "", None, 1, events, visitor_joined)
        self.assertEqual(events[0]["members"], [1])
        self.assertEqual(events[0]["post_id"], 123)
        apply_event("leave_event", 1, 1, "", None, 2, events, visitor_joined)
        self.assertEqual(events[0]["members"], [])
        self.assertEqual(visitor_joined, [])
