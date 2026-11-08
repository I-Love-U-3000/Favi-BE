"""Schema-only token savings, without changing callable names or arguments."""
import copy
from contextlib import closing
import functools
import inspect
import sqlite3

ID_COLUMNS = {"post_id": ("post", "post_id"), "comment_id": ("comment", "comment_id"),
              "group_id": ("chat_group", "group_id"), "followee_id": ("user", "user_id"),
              "mutee_id": ("user", "user_id"), "user_id": ("user", "user_id")}


def compact_schema(schema):
    result = copy.deepcopy(schema)
    function = result["function"]
    function["description"] = function.get("description", "").splitlines()[0]
    for parameter in function.get("parameters", {}).get("properties", {}).values():
        if "description" in parameter:
            parameter["description"] = " ".join(parameter["description"].split())
    return result


def reflection_tool(state, audit, actor_id):
    def private_reflection(persona: str, emotion: str, goal: str, experience: str) -> dict:
        """Privately retain or revise your own identity, feelings, goal and experience; never publish or control others.

        Args:
            persona: Your current self-description.
            emotion: Your current feelings.
            goal: Your current aim.
            experience: Your own private fictional experience or reflection.
        """
        values = {"persona": persona, "emotion": emotion, "goal": goal, "experience": experience}
        if any(not isinstance(value, str) or len(value) > 4096 for value in values.values()):
            return {"success": False, "error": "State fields must be text within the memory budget"}
        if any(not values[key].strip() for key in ("persona", "emotion", "goal")):
            return {"success": False, "error": "Identity, feelings and goal cannot be empty"}
        state.update(values)
        audit.append({"actor": actor_id, **values})
        return {"success": True, "private_state": dict(state)}
    return private_reflection


def guarded_callable(function, actor_id, database, attempts):
    """API integrity boundary; never choose or replace the model's action."""
    signature = inspect.signature(function)
    for name in signature.parameters:
        if name.endswith("_id") and name not in ID_COLUMNS:
            raise ValueError(f"Unreviewed native ID parameter: {name}")

    @functools.wraps(function)
    async def guarded(*args, **kwargs):
        entry = {"actor": actor_id, "tool": function.__name__, "arguments": kwargs}
        attempts.append(entry)
        try:
            arguments = signature.bind(*args, **kwargs).arguments
            entry["arguments"] = dict(arguments)
            with closing(sqlite3.connect(database.resolve().as_uri() + "?mode=ro", uri=True)) as db:
                for name, value in arguments.items():
                    if name not in ID_COLUMNS:
                        continue
                    table, key = ID_COLUMNS[name]
                    if type(value) is not int or not db.execute(f'SELECT 1 FROM "{table}" WHERE "{key}"=?', (value,)).fetchone():
                        entry["result"] = {"success": False, "error": f"Unknown {name}; discover actual IDs using platform tools"}
                        return entry["result"]
                if function.__name__ in {"follow", "unfollow"} and arguments.get("followee_id") == actor_id:
                    entry["result"] = {"success": False, "error": "Cannot follow or unfollow yourself"}
                    return entry["result"]
            result = function(*args, **kwargs)
            entry["result"] = await result if inspect.isawaitable(result) else result
            return entry["result"]
        except Exception as error:
            entry["error"] = f"{type(error).__name__}: {error}"
            raise
    return guarded
