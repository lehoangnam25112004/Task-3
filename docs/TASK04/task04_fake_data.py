"""Create or safely remove the isolated TASK-04 sample data."""

import argparse
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path


TAG = "[TEST_TASK04]"
CODE = "WBS-TEST-"
PROJECT_CODE = "FAKE-TASK03-P01"
MANAGER = "fake.task03.manager@example.local"
MIGRATION = "20261122070416_Task04WbsLifecycle"


def quote(identifier):
    return '"' + identifier.replace('"', '""') + '"'


def ids_clause(ids):
    return ",".join("?" for _ in ids)


def backup(db_path, state_dir, operation):
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    destination = state_dir / f"task04-{operation}-{stamp}.db"
    with destination.open("xb"):
        pass
    with sqlite3.connect(db_path.as_uri() + "?mode=ro", uri=True) as source:
        with sqlite3.connect(destination) as target:
            source.backup(target)
            if target.execute("PRAGMA integrity_check").fetchone()[0] != "ok":
                raise RuntimeError(f"Backup integrity check failed: {destination}")
    print(f"SQLite backup: {destination}")


def require_clean_fk(db):
    violations = db.execute("PRAGMA foreign_key_check").fetchall()
    if violations:
        raise RuntimeError(f"Foreign key violations detected: {violations[:5]}")


def load_manifest(path, db_path):
    data = json.loads(path.read_text(encoding="utf-8"))
    if Path(data["database"]).resolve() != db_path:
        raise RuntimeError("Manifest belongs to another database; refusing to proceed.")
    return data


def assert_owned(db, manifest, allow_missing=False):
    missing = 0
    total = 0
    for table, records, marker_column, marker in [
        ("ProjectPhases", manifest["phases"], "Name", TAG),
        ("WorkItems", manifest["items"], "Description", TAG),
        ("Tasks", manifest["tasks"], "Description", TAG),
    ]:
        for record_id in records.values():
            total += 1
            row = db.execute(
                f"SELECT ProjectId, {quote(marker_column)} FROM {quote(table)} WHERE Id = ?",
                (record_id,),
            ).fetchone()
            if row is None:
                missing += 1
            elif row[0] != manifest["project_id"] or not (row[1] or "").startswith(marker):
                raise RuntimeError(f"{table} #{record_id} lost its ownership marker or changed project.")
    if missing and not (allow_missing and missing == total):
        raise RuntimeError("Sample records are missing or incomplete; inspect before proceeding.")
    return missing == total


def insert_phase(db, project_id, name, sequence, archived, group, actor, now):
    return db.execute(
        """INSERT INTO ProjectPhases
           (ProjectId, Name, SequenceOrder, Status, IsArchived, StatusBeforeArchive,
            ArchiveGroupId, ArchivedAt, ArchivedByUserId)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (project_id, TAG + " " + name, sequence, 3 if archived else 1, int(archived),
         1 if archived else None, group if archived else None,
         now if archived else None, actor if archived else None),
    ).lastrowid


def insert_item(db, project_id, phase_id, parent, code, name, level, order, status,
                actor, now, group=None, previous=None):
    archived = status == 4
    return db.execute(
        """INSERT INTO WorkItems
           (ProjectId, ProjectPhaseId, ParentWorkItemId, WbsCode, Name, Description,
            Level, SequenceOrder, Status, IsArchived, StatusBeforeArchive, ArchiveGroupId,
            ArchivedAt, ArchivedByUserId, CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (project_id, phase_id, parent, CODE + code, TAG + " " + name, TAG + " Dữ liệu giả lập WBS",
         level, order, status, int(archived), previous if archived else None,
         group if archived else None, now if archived else None, actor if archived else None,
         now, actor, now if archived else None, actor if archived else None),
    ).lastrowid


def seed(db, db_path, state_dir, manifest_path):
    if manifest_path.exists():
        manifest = load_manifest(manifest_path, db_path)
        assert_owned(db, manifest)
        print("Dữ liệu TASK-04 đã tồn tại; không ghi lại.")
        return
    project = db.execute(
        "SELECT Id, Status, IsArchived FROM Projects WHERE ProjectCode = ?", (PROJECT_CODE,)
    ).fetchone()
    if project is None or project[1] != 2 or project[2]:
        raise RuntimeError("Dự án mẫu không tồn tại hoặc không còn đang thực hiện.")
    project_id = project[0]
    actor = db.execute(
        """SELECT u.Id FROM AspNetUsers u
           JOIN AspNetUserRoles ur ON ur.UserId = u.Id
           JOIN AspNetRoles r ON r.Id = ur.RoleId
           JOIN ProjectMembers m ON m.UserId = u.Id
           WHERE u.UserName = ? AND u.IsActive = 1 AND r.Name = 'Project Manager'
             AND m.ProjectId = ? AND m.IsActive = 1""", (MANAGER, project_id)
    ).fetchone()
    if actor is None:
        raise RuntimeError("Tài khoản PM mẫu không phải thành viên active của dự án.")
    for table, column, prefix in [
        ("ProjectPhases", "Name", TAG), ("WorkItems", "WbsCode", CODE),
        ("Tasks", "Description", TAG),
    ]:
        if db.execute(
            f"SELECT 1 FROM {quote(table)} WHERE substr({quote(column)},1,?) = ? LIMIT 1",
            (len(prefix), prefix),
        ).fetchone():
            raise RuntimeError(f"Đã có dữ liệu đánh dấu {prefix} trong {table}; không ghi đè.")
    if not db.execute("SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = ?", (MIGRATION,)).fetchone():
        raise RuntimeError("Thiếu migration WBS bắt buộc.")
    backup(db_path, state_dir, "before-seed")
    actor = actor[0]
    now = datetime.now(timezone.utc).replace(tzinfo=None).isoformat()
    group, separate, linked_archive = (str(uuid.uuid4()).upper() for _ in range(3))
    phases = {}
    items = {}
    tasks = {}
    phases["active"] = insert_phase(db, project_id, "Thi công đang thực hiện", 10, False, None, actor, now)
    phases["archive"] = insert_phase(db, project_id, "Nhóm đã lưu trữ", 20, True, group, actor, now)

    def item(key, phase, parent, code, name, level, order, status, archive_group=None, previous=None):
        items[key] = insert_item(db, project_id, phases[phase], items.get(parent), code, name,
                                 level, order, status, actor, now, archive_group, previous)

    item("root", "active", None, "01", "Thi công phần móng", 1, 10, 1)
    item("child", "active", "root", "01.01", "Công tác đất chưa bắt đầu", 2, 10, 0)
    item("grandchild", "active", "child", "01.01.01", "Chờ khảo sát", 3, 10, 3)
    item("completed", "active", "root", "01.02", "Chuẩn bị mặt bằng hoàn thành", 2, 20, 2)
    item("active2", "active", None, "02", "Theo dõi vật tư đang thực hiện", 1, 20, 1)
    item("linked_archive", "active", None, "03", "Hạng mục lưu trữ có Task", 1, 30, 4, linked_archive, 3)
    item("group_root", "archive", None, "10", "Nhánh lưu trữ theo giai đoạn", 1, 10, 4, group, 1)
    item("group_child", "archive", "group_root", "10.01", "Con lưu trữ cùng đợt", 2, 10, 4, group, 3)
    item("separate", "archive", "group_root", "10.02", "Con đã lưu trữ riêng", 2, 20, 4, separate, 2)

    for key, title, item_key in [
        ("active", "Kiểm tra công tác đất", "child"),
        ("active2", "Theo dõi vật tư", "active2"),
        ("archived", "Kiểm tra liên kết WBS đã lưu trữ", "linked_archive"),
    ]:
        tasks[key] = db.execute(
            """INSERT INTO Tasks (ProjectId, WorkItemId, Title, Description, Priority,
               PlannedStartDate, PlannedEndDate, Status, PercentComplete, RequiresInspection,
               CreatedAt, UpdatedAt) VALUES (?, ?, ?, ?, 1, ?, ?, 0, '0', 0, ?, ?)""",
            (project_id, items[item_key], TAG + " " + title, TAG + " Dữ liệu giả lập Task",
             "2026-11-22T00:00:00", "2026-12-22T00:00:00", now, now),
        ).lastrowid
    manifest = {"database": str(db_path), "project_id": project_id, "phases": phases,
                "items": items, "tasks": tasks, "archive_group": group,
                "separate_group": separate, "linked_archive_group": linked_archive}
    require_clean_fk(db)
    with manifest_path.open("x", encoding="utf-8") as file:
        json.dump(manifest, file, ensure_ascii=False, indent=2)
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


def refuse_external_references(db, owned):
    tables = [row[0] for row in db.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
    )]
    for table in tables:
        for fk in db.execute(f"PRAGMA foreign_key_list({quote(table)})").fetchall():
            target, source_column, target_column = fk[2], fk[3], fk[4]
            if target not in owned or not owned[target]:
                continue
            if target_column not in (None, "Id") or fk[1] != 0:
                raise RuntimeError(f"Unsupported foreign key from {table}; inspect manually.")
            targets = list(owned[target])
            sql = (f"SELECT COUNT(*) FROM {quote(table)} WHERE {quote(source_column)} "
                   f"IN ({ids_clause(targets)})")
            args = targets
            if table in owned and owned[table]:
                source_ids = list(owned[table])
                sql += f" AND Id NOT IN ({ids_clause(source_ids)})"
                args = targets + source_ids
            if db.execute(sql, args).fetchone()[0]:
                raise RuntimeError(f"External references from {table}.{source_column}; cleanup refused.")
            if table == "Tasks" and target == "Tasks":
                if db.execute(
                    f"SELECT COUNT(*) FROM Tasks WHERE {quote(source_column)} IN ({ids_clause(targets)})",
                    targets,
                ).fetchone()[0]:
                    raise RuntimeError("Task hierarchy exists; inspect before cleanup.")


def cleanup(db, db_path, state_dir, manifest_path):
    if not manifest_path.exists():
        print("Không có manifest TASK-04; không xóa dữ liệu.")
        return False
    manifest = load_manifest(manifest_path, db_path)
    if assert_owned(db, manifest, allow_missing=True):
        print("Các bản ghi trong manifest đã không còn; không xóa dữ liệu.")
        return True
    owned = {"ProjectPhases": set(manifest["phases"].values()),
             "WorkItems": set(manifest["items"].values()),
             "Tasks": set(manifest["tasks"].values())}
    refuse_external_references(db, owned)
    backup(db_path, state_dir, "before-cleanup")
    for task_id in owned["Tasks"]:
        db.execute("DELETE FROM Tasks WHERE Id = ?", (task_id,))
    remaining = set(owned["WorkItems"])
    while remaining:
        leaves = [item_id for item_id in remaining if not db.execute(
            "SELECT 1 FROM WorkItems WHERE ParentWorkItemId = ? LIMIT 1", (item_id,)
        ).fetchone()]
        if not leaves:
            raise RuntimeError("Cây WBS có vòng lặp hoặc phụ thuộc; cleanup bị hủy.")
        for item_id in leaves:
            db.execute("DELETE FROM WorkItems WHERE Id = ?", (item_id,))
            remaining.remove(item_id)
    for phase_id in owned["ProjectPhases"]:
        db.execute("DELETE FROM ProjectPhases WHERE Id = ?", (phase_id,))
    require_clean_fk(db)
    print("Đã xóa dữ liệu theo manifest, không thay đổi bản ghi khác.")
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("seed", "cleanup"))
    parser.add_argument("--database", required=True, type=Path)
    parser.add_argument("--state-dir", required=True, type=Path)
    args = parser.parse_args()
    db_path = args.database.resolve(strict=True)
    state_dir = args.state_dir.resolve(strict=True)
    if not state_dir.is_dir():
        raise RuntimeError("State directory must exist.")
    manifest_path = state_dir / "task04-fake-manifest.json"
    db = sqlite3.connect(db_path, timeout=10)
    db.execute("PRAGMA foreign_keys = ON")
    manifest_created = False
    try:
        db.execute("BEGIN IMMEDIATE")
        require_clean_fk(db)
        if args.operation == "seed":
            existed = manifest_path.exists()
            seed(db, db_path, state_dir, manifest_path)
            manifest_created = not existed and manifest_path.exists()
            db.commit()
        else:
            remove_manifest = cleanup(db, db_path, state_dir, manifest_path)
            db.commit()
            if remove_manifest:
                archived_name = state_dir / f"task04-cleaned-{uuid.uuid4()}.json"
                manifest_path.rename(archived_name)
                print(f"Manifest lưu lại: {archived_name}")
    except Exception:
        db.rollback()
        if manifest_created:
            print(f"Transaction đã rollback; kiểm tra và xóa manifest chưa sử dụng: {manifest_path}", file=sys.stderr)
        raise
    finally:
        db.close()


if __name__ == "__main__":
    try:
        main()
    except (OSError, sqlite3.Error, ValueError, KeyError, RuntimeError) as error:
        print(f"LỖI: {error}", file=sys.stderr)
        sys.exit(1)
