# -*- coding: utf-8 -*-
"""
Dopisuje transkrypt sesji Claude Code do Docs/Konwersacja_RubikND.md.

Wyciąga tury tekstowe użytkownika/asystenta (pomija wywołania narzędzi, system-remindery i szum),
scala kolejne tury tej samej roli i dokleja w formacie '## 🧑 Robert' / '## 🤖 Claude'.

Użycie (z dowolnego miejsca):
    python append_convo.py                  # DRY-RUN, najnowsza sesja projektu
    python append_convo.py --write          # faktyczne dopisanie
    python append_convo.py --session <id>   # konkretna sesja (nazwa pliku bez .jsonl)
    python append_convo.py --file <ścieżka> # jawna ścieżka do .jsonl
    python append_convo.py --label "opis"   # dopisek do znacznika sesji
    python append_convo.py --dst <ścieżka>  # inny plik docelowy .md

Transkrypty leżą w:  ~/.claude/projects/<zakodowana-nazwa-projektu>/<session-id>.jsonl
Ponowne uruchomienie na tej samej sesji jest bezpieczne — znacznik '<!-- Sesja <id> -->' w pliku
docelowym zapobiega podwójnemu dopisaniu.
"""
import os, glob, json, re, sys, argparse

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_DST = os.path.normpath(os.path.join(HERE, "..", "Konwersacja_RubikND.md"))

SKIP = ('<system-reminder>', 'This session is being continued', '<command-name>', '<command-message>',
        '<local-command-stdout>', 'Caveat:', '<command-args>', '[Request interrupted', '<system>')


def find_projects_dir():
    home = os.path.expanduser("~")
    for d in glob.glob(os.path.join(home, ".claude", "projects", "*RubikCubeND*")):
        if glob.glob(os.path.join(d, "*.jsonl")):
            return d
    return None


def pick_jsonl(args):
    if args.file:
        return args.file
    pdir = find_projects_dir()
    if not pdir:
        sys.exit("Nie znaleziono katalogu .claude/projects projektu.")
    if args.session:
        p = os.path.join(pdir, args.session + ".jsonl")
        if not os.path.exists(p):
            sys.exit("Brak takiej sesji: " + p)
        return p
    files = glob.glob(os.path.join(pdir, "*.jsonl"))
    if not files:
        sys.exit("Brak plików .jsonl w " + pdir)
    return max(files, key=os.path.getmtime)   # najnowsza sesja


def text_of(content):
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return "\n".join(b.get('text', '') for b in content
                         if isinstance(b, dict) and b.get('type') == 'text')
    return ""


def is_noise(t):
    s = t.strip()
    return (not s) or any(s.startswith(m) for m in SKIP)


def extract(path):
    turns = []
    with open(path, encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                o = json.loads(line)
            except Exception:
                continue
            if o.get('type') not in ('user', 'assistant'):
                continue
            msg = o.get('message', {})
            if not isinstance(msg, dict):
                continue
            txt = text_of(msg.get('content'))
            txt = re.sub(r'<system-reminder>.*?</system-reminder>', '', txt, flags=re.S).strip()
            if o['type'] == 'user':
                if is_noise(txt) or not txt:
                    continue
                turns.append(('user', txt))
            elif txt.strip():
                turns.append(('assistant', txt.strip()))
    merged = []
    for role, txt in turns:
        if merged and merged[-1][0] == role:
            merged[-1] = (role, merged[-1][1] + "\n\n" + txt)
        else:
            merged.append((role, txt))
    return merged


def session_done_count(existing, sid):
    """Ile tur tej sesji jest już w pliku docelowym (liczone po nagłówkach za znacznikiem sesji)."""
    m = re.search(r'<!-- Sesja[^>\n]*' + re.escape(sid[:8]) + r'[^>]*-->', existing)
    if not m:
        return 0
    rest = existing[m.end():]
    nd = re.search(r'<!-- Sesja ', rest)               # następny znacznik sesji (jeśli jest)
    seg = rest[:nd.start()] if nd else rest
    return len(re.findall(r'(?m)^## .*(?:Robert|Claude)\s*$', seg))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--write', action='store_true', help='faktycznie dopisz (domyślnie dry-run)')
    ap.add_argument('--resume', action='store_true', help='dopisz tylko NOWE tury tej sesji (tryb dla Stop hooka)')
    ap.add_argument('--session', help='id sesji (nazwa pliku bez .jsonl)')
    ap.add_argument('--file', help='jawna ścieżka do .jsonl')
    ap.add_argument('--dst', default=DEFAULT_DST, help='plik docelowy .md')
    ap.add_argument('--label', default='', help='dopisek do znacznika sesji')
    args = ap.parse_args()

    src = pick_jsonl(args)
    sid = os.path.splitext(os.path.basename(src))[0]
    merged = extract(src)
    if not merged:
        return
    existing = open(args.dst, encoding='utf-8').read() if os.path.exists(args.dst) else ''

    U, A = "## 🧑 Robert", "## 🤖 Claude"
    fmt = lambda ts: "\n".join(f"\n{U if r == 'user' else A}\n\n{txt}\n" for r, txt in ts)
    note = (" " + args.label) if args.label else ""
    divider = "\n\n---\n\n<!-- Sesja " + sid + note + " -->\n"

    if args.resume:
        done = session_done_count(existing, sid)
        new = merged[done:]
        if not new:
            return                                       # cicho: hook odpala co turę
        block = (divider if done == 0 else "") + fmt(new)
        if not args.write:
            print(f"[DRY-RUN resume] {len(new)} nowych tur, {len(block)} zn. -> {args.dst}")
            return
        with open(args.dst, 'a', encoding='utf-8') as f:
            f.write(block)
        return                                           # cicho: Stop hook odpala co turę

    # tryb pełny: cała sesja naraz, dedup po znaczniku (całe id lub krótkie)
    if ("<!-- Sesja " + sid) in existing or re.search(r'<!-- Sesja[^>\n]*' + re.escape(sid[:8]), existing):
        print("Sesja", sid, "już dopisana. Nic do zrobienia.")
        return
    block = divider + fmt(merged)
    print("source:", src, "\nturns :", len(merged))
    if not args.write:
        print(f"[DRY-RUN] dopisałbym {len(merged)} tur, {len(block)} zn. Uruchom z --write.")
        return
    with open(args.dst, 'a', encoding='utf-8') as f:
        f.write(block)
    print(f"DOPISANO {len(merged)} tur -> {args.dst}")


if __name__ == '__main__':
    try:
        main()
    except OSError as e:
        # plik docelowy zajęty (otwarty w edytorze) lub inny błąd I/O -> nie wywalaj Stop hooka;
        # --resume liczy od stanu pliku, więc następna tura dopisze to, co teraz nie weszło.
        sys.stderr.write("append_convo: " + str(e) + "\n")
        sys.exit(0)
