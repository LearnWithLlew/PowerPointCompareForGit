#!/usr/bin/env python3
"""Renders every deck in the corpus with both renderers, as whole decks and as one PNG per slide.

Copies the decks into a new git repo, so the source repos are never touched and the previews land in
the copy's .git folder. Results go to <root>/results/runs.jsonl as they finish, and re-running skips
what is already done, so this can be stopped and resumed.

usage: run_corpus.py ROOT DECK_FOLDER...
"""
import json, os, shutil, signal, subprocess, sys, threading, time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

WORKERS = 4
TIMEOUT_SECONDS = 45 * 60
RENDERERS = ["LibreOffice", "DeckCraft"]
MODES = {"pages": ["--pages", "1-9999"], "whole": []}

root = Path(sys.argv[1]).resolve()
sources = [Path(folder).resolve() for folder in sys.argv[2:]]
corpus = root / "corpus"
tool = root / "tool" / "PowerPointCompareForGit.dll"
results = root / "results" / "runs.jsonl"
lock = threading.Lock()


def git(*args):
    subprocess.run(["git", "-c", "user.name=experiment", "-c", "user.email=experiment@example.com", *args], cwd=corpus, check=True)


def prepare():
    if (corpus / ".git").exists():
        return
    corpus.mkdir(parents=True)
    git("init", "-q")
    for source in sources:
        for deck in source.rglob("*.pptx"):
            relative = deck.relative_to(source)
            if ".git" in relative.parts or deck.name.startswith("~$"):
                continue
            target = corpus / source.name / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(deck, target)
    git("add", "-A")
    git("commit", "-qm", "corpus")


def done():
    if not results.exists():
        return set()
    return {(r["deck"], r["renderer"], r["mode"]) for r in map(json.loads, results.read_text().splitlines())}


def run(job):
    deck, renderer, mode = job
    start = time.time()
    process = subprocess.Popen(
        ["dotnet", str(tool), deck, "--renderer", renderer, *MODES[mode]],
        cwd=corpus, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, start_new_session=True)
    try:
        out, err = process.communicate(timeout=TIMEOUT_SECONDS)
        exit_code = process.returncode
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        out, err = process.communicate()
        exit_code = "timeout"
    record = {"deck": deck, "renderer": renderer, "mode": mode, "exit": exit_code, "seconds": round(time.time() - start, 1),
              "outputs": [line for line in out.splitlines() if line.endswith(".png")], "error": err.strip()[-2000:]}
    with lock:
        with results.open("a") as file:
            file.write(json.dumps(record) + "\n")
        print(f"{exit_code} {record['seconds']:>8}s {renderer:<12}{mode:<6} {deck}", flush=True)


prepare()
results.parent.mkdir(exist_ok=True)
decks = sorted(subprocess.run(["git", "ls-files", "*.pptx"], cwd=corpus, capture_output=True, text=True, check=True,
                              env={**os.environ, "GIT_CONFIG_PARAMETERS": "'core.quotepath=false'"}).stdout.splitlines(),
               key=lambda deck: (corpus / deck).stat().st_size)
finished = done()
# DeckCraft first within each deck: it is much faster, so its results arrive early.
jobs = [(deck, renderer, mode) for deck in decks for renderer in reversed(RENDERERS) for mode in MODES if (deck, renderer, mode) not in finished]
print(f"{len(decks)} decks, {len(jobs)} runs to do", flush=True)
with ThreadPoolExecutor(WORKERS) as pool:
    list(pool.map(run, jobs))
print("finished", flush=True)
