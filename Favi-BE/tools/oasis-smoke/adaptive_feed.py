"""Explicit offline input contract for production-shaped cold/warm ranking."""
import uuid
from adaptive_onboarding import TOPICS, visible_preferences, classify_cohort
from benchmark_recommenders import Ranker
from favi_rankers import cold_rank, warm_rank


def source_guid(value):
    # Positive post IDs and zero-based actor IDs use separate logical domains.
    return str(uuid.UUID(int=value))


class AdaptiveFeed:
    def __init__(self, events, cutoff, onboarding, *, warm_model=None, threshold=1,
                 method='formula', hours_per_tick=1.0, parameters=None):
        if threshold not in (1, 5) or method not in ('formula', 'formula-v2', 'popularity', 'topic-only', 'collaborative'):
            raise ValueError('Unsupported route')
        if method == 'collaborative' and (warm_model is None or warm_model.cutoff != cutoff):
            raise ValueError('Collaborative model required at same cutoff')
        self.base = Ranker(events, cutoff, 'favi_formula', hours_per_tick)
        self.cutoff, self.items, self.seen, self.likes = cutoff, self.base.items, self.base.seen, self.base.likes
        if len({r['user_id'] for r in onboarding}) != len(onboarding):
            raise ValueError('Duplicate onboarding user')
        self.onboarding = {r['user_id']: r for r in onboarding}
        self.warm_model, self.threshold, self.method = warm_model, threshold, method
        self.hours_per_tick = hours_per_tick
        self.v2 = (Ranker(events, cutoff, 'favi_formula_v2', hours_per_tick, parameters)
                   if method == 'formula-v2' else None)

    def candidates(self, user):
        row = self.onboarding.get(user)
        if row is None or row['arrival_tick'] >= self.cutoff:
            return set()
        return self.base.candidates(user)

    def topics(self, user):
        row = self.onboarding.get(user)
        declared = visible_preferences(row, self.cutoff) if row else None
        return declared if declared else list(TOPICS)

    def cohort(self, user):
        row = self.onboarding.get(user)
        declared = visible_preferences(row, self.cutoff) if row else None
        if declared is None:
            return None
        matching = sum(TOPICS[self.items[i]['topic']] in declared for i in self.candidates(user))
        return classify_cohort(len(self.seen.get(user, ())), declared, matching)

    def cold(self, user, k):
        # Match reader's default graph-empty pool construction under common
        # offline eligibility. Actual SQL authorization is outside this model.
        eligible = self.candidates(user)
        ordered = sorted(eligible, key=lambda i: (-(self.likes[i] + 2 * self.base.comments[i]), -self.items[i]['tick'], i))
        topics = self.topics(user)
        pool = []
        def add(item, score):
            p = self.items[item]
            pool.append(dict(id=source_guid(item), author=source_guid(p['user_id'] + 1),
                             score=score, topics=[TOPICS[p['topic']]]))
        for topic in topics:
            matches = [i for i in ordered if TOPICS[self.items[i]['topic']] == topic][:20]
            for index, item in enumerate(matches):
                add(item, -index)
        for index, item in enumerate(ordered[:60]):
            add(item, -100 - index)
        return [uuid.UUID(i).int for i in cold_rank(pool, topics, k)]

    def rank(self, user, k):
        if k <= 0 or not self.candidates(user):
            return []
        if self.method == 'topic-only' or (self.method != 'popularity' and len(self.seen.get(user, ())) < self.threshold):
            return self.cold(user, k)
        if self.method == 'popularity':
            return sorted(self.candidates(user), key=lambda i: (-self.likes[i], i))[:k]
        if self.method == 'collaborative':
            return self.warm_model.rank(user, k)
        if self.method == 'formula-v2':
            return self.v2.rank(user, k)
        followed = {source_guid(a + 1) for a in self.base.follows.get(user, ())}
        followers = {source_guid(a + 1) for a, followed_ids in self.base.follows.items() if user in followed_ids}
        interactions = {source_guid(author + 1): count for (viewer, author), count in self.base.author_interactions.items() if viewer == user}
        posts = []
        for item in sorted(self.candidates(user)):
            p = self.items[item]
            posts.append(dict(id=source_guid(item), author=source_guid(p['user_id'] + 1), self=False,
                              reactions=self.likes[item], comments=self.base.comments[item], tags=1,
                              created=p['tick'] * self.hours_per_tick))
        ranked, _ = warm_rank(posts, followed, followers, interactions, self.cutoff * self.hours_per_tick)
        return [uuid.UUID(i).int for i in ranked[:k]]
