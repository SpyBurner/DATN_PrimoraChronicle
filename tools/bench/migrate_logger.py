#!/usr/bin/env python3
"""
Migrates IDebugLogger call sites from the old single-arg signature
    _logger?.Log("[Tag] message")
to the new four-arg signature
    _logger?.Log("LOG_TAG", nameof(ClassName), "message", 0)

- ClassName is detected from the first `class Foo` declaration in the file.
- If the message starts with `[Tag]`, Tag (uppercase) becomes the LOG_CODE
  suffix and is stripped from the message.
- Otherwise LOG_CODE falls back to LOG_<CLASSNAME_UPPER>.
- Handles both `_logger.Log(...)` and `_logger?.Log(...)`, and both regular
  and `$""` interpolated strings.
- Preserves whitespace and a trailing semicolon.
- Skips re-migration if the call already has 3+ args.

Run from repo root:
    py -3 tools/bench/migrate_logger.py
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Assets" / "_Game"

LOGGER_CALL = re.compile(r'(_logger\??\.)(Log|LogWarning|LogError)\(')


CLASS_DECL = re.compile(r'\bclass\s+([A-Za-z_][A-Za-z0-9_]*)')


def find_class_spans(text: str):
    """Return list of (start_idx, end_idx, name) for every class in the file.
    end_idx is the index of the closing brace of that class."""
    spans = []
    for m in CLASS_DECL.finditer(text):
        # Find opening brace after the declaration
        open_idx = text.find('{', m.end())
        if open_idx < 0:
            continue
        depth = 1
        j = open_idx + 1
        while j < len(text) and depth > 0:
            c = text[j]
            if c == '{':
                depth += 1
            elif c == '}':
                depth -= 1
                if depth == 0:
                    break
            elif c == '"':
                # skip string
                is_interp = j > 0 and text[j-1] == '$'
                j += 1
                while j < len(text):
                    cc = text[j]
                    if cc == '\\' and not is_interp:
                        j += 2
                        continue
                    if cc == '"':
                        if j+1 < len(text) and text[j+1] == '"':
                            j += 2
                            continue
                        j += 1
                        break
                    if cc == '{' and is_interp:
                        brace = 1
                        j += 1
                        while j < len(text) and brace:
                            if text[j] == '{':
                                brace += 1
                            elif text[j] == '}':
                                brace -= 1
                            j += 1
                        continue
                    j += 1
                continue
            elif c == "'":
                j += 1
                while j < len(text) and text[j] != "'":
                    if text[j] == '\\':
                        j += 1
                    j += 1
                j += 1
                continue
            elif c == '/' and j + 1 < len(text) and text[j+1] == '/':
                while j < len(text) and text[j] != '\n':
                    j += 1
                continue
            elif c == '/' and j + 1 < len(text) and text[j+1] == '*':
                j += 2
                while j + 1 < len(text) and not (text[j] == '*' and text[j+1] == '/'):
                    j += 1
                j += 2
                continue
            j += 1
        spans.append((m.start(), j, m.group(1)))
    return spans


def enclosing_class(spans, pos: int) -> str:
    """Find the innermost class span containing `pos`."""
    best = None
    best_len = None
    for s, e, name in spans:
        if s <= pos <= e:
            length = e - s
            if best is None or length < best_len:
                best = name
                best_len = length
    return best or "Unknown"


def find_matching_paren(text: str, open_idx: int) -> int:
    """Given index of `(`, return index of the matching `)`.
    Tracks string literals, $-interpolation braces, and char literals.
    Returns -1 on failure."""
    depth = 1
    i = open_idx + 1
    while i < len(text):
        c = text[i]
        if c == '(':
            depth += 1
            i += 1
        elif c == ')':
            depth -= 1
            if depth == 0:
                return i
            i += 1
        elif c == '"':
            # consume string literal (incl. interpolated)
            # check for $"
            is_interp = i > 0 and text[i-1] == '$'
            i += 1
            while i < len(text):
                ch = text[i]
                if ch == '\\' and not is_interp:
                    i += 2
                    continue
                if ch == '"':
                    # double-quote escape in verbatim or interp? skip
                    if i+1 < len(text) and text[i+1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                if ch == '{' and is_interp:
                    # interpolation, skip to matching }
                    brace = 1
                    i += 1
                    while i < len(text) and brace:
                        cc = text[i]
                        if cc == '{':
                            brace += 1
                        elif cc == '}':
                            brace -= 1
                        elif cc == '"':
                            # nested string in interp
                            i += 1
                            while i < len(text) and text[i] != '"':
                                if text[i] == '\\':
                                    i += 1
                                i += 1
                        i += 1
                    continue
                i += 1
        elif c == "'":
            # char literal
            i += 1
            while i < len(text) and text[i] != "'":
                if text[i] == '\\':
                    i += 1
                i += 1
            i += 1
        elif c == '/' and i + 1 < len(text) and text[i+1] == '/':
            # line comment
            while i < len(text) and text[i] != '\n':
                i += 1
        elif c == '/' and i + 1 < len(text) and text[i+1] == '*':
            # block comment
            i += 2
            while i + 1 < len(text) and not (text[i] == '*' and text[i+1] == '/'):
                i += 1
            i += 2
        else:
            i += 1
    return -1


def split_top_level_commas(args: str) -> list:
    """Split a string at top-level commas (ignoring those nested in strings/braces/parens)."""
    parts = []
    depth_paren = 0
    depth_brace = 0
    depth_brack = 0
    i = 0
    start = 0
    n = len(args)
    while i < n:
        c = args[i]
        if c == '(':
            depth_paren += 1
        elif c == ')':
            depth_paren -= 1
        elif c == '{':
            depth_brace += 1
        elif c == '}':
            depth_brace -= 1
        elif c == '[':
            depth_brack += 1
        elif c == ']':
            depth_brack -= 1
        elif c == '"':
            # skip string
            is_interp = i > 0 and args[i-1] == '$'
            i += 1
            while i < n:
                ch = args[i]
                if ch == '\\' and not is_interp:
                    i += 2
                    continue
                if ch == '"':
                    if i+1 < n and args[i+1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                if ch == '{' and is_interp:
                    brace = 1
                    i += 1
                    while i < n and brace:
                        if args[i] == '{':
                            brace += 1
                        elif args[i] == '}':
                            brace -= 1
                        i += 1
                    continue
                i += 1
            continue
        elif c == "'":
            i += 1
            while i < n and args[i] != "'":
                if args[i] == '\\':
                    i += 1
                i += 1
            i += 1
            continue
        elif c == ',' and depth_paren == 0 and depth_brace == 0 and depth_brack == 0:
            parts.append(args[start:i])
            start = i + 1
        i += 1
    parts.append(args[start:])
    return parts


BRACKET_RE = re.compile(r'^\s*\[([A-Za-z][A-Za-z0-9_ ]*)\]\s*')


def derive_log_code(msg_literal: str, class_name: str) -> tuple[str, str]:
    """Given a raw string literal (quoted), return (LOG_CODE, stripped_literal).
    Strips a leading `[Tag] ` from the message and uses Tag as the suffix."""
    # Strip outer quotes (could be $"..." or "..." )
    inner = msg_literal
    prefix = ""
    if inner.startswith('$"'):
        prefix = '$"'
        body = inner[2:-1]
    elif inner.startswith('"'):
        prefix = '"'
        body = inner[1:-1]
    else:
        # Not a literal — leave alone
        return f"LOG_{class_name.upper()}", msg_literal

    m = BRACKET_RE.match(body)
    if m:
        tag = m.group(1).strip().upper().replace(' ', '_').replace('-', '_')
        new_body = body[m.end():]
        code = f"LOG_{tag}"
        return code, f'{prefix}{new_body}"'
    return f"LOG_{class_name.upper()}", msg_literal


def process_file(path: Path) -> int:
    text = path.read_text(encoding='utf-8')
    if '_logger' not in text or '.Log' not in text:
        return 0
    spans = find_class_spans(text)

    output = []
    i = 0
    n = len(text)
    changes = 0

    while i < n:
        m = LOGGER_CALL.search(text, i)
        if not m:
            output.append(text[i:])
            break
        output.append(text[i:m.start()])

        method = m.group(2)
        open_idx = m.end() - 1  # index of `(`
        close_idx = find_matching_paren(text, open_idx)
        if close_idx < 0:
            # malformed; emit raw
            output.append(text[m.start():m.end()])
            i = m.end()
            continue

        args_str = text[open_idx+1:close_idx]
        parts = split_top_level_commas(args_str)
        parts = [p.strip() for p in parts]

        # Skip if already 3+ args (already migrated)
        if len(parts) >= 3:
            output.append(text[m.start():close_idx+1])
            i = close_idx + 1
            continue

        # Expect 1 arg (the message). Rebuild as 3 args.
        if len(parts) != 1 or not parts[0]:
            output.append(text[m.start():close_idx+1])
            i = close_idx + 1
            continue

        msg = parts[0]
        cls = enclosing_class(spans, m.start())
        log_code, stripped_msg = derive_log_code(msg, cls)

        new_call = f'{m.group(1)}{method}("{log_code}", nameof({cls}), {stripped_msg})'
        output.append(new_call)
        changes += 1
        i = close_idx + 1

    if changes:
        path.write_text(''.join(output), encoding='utf-8')
    return changes


def main():
    total_files = 0
    total_changes = 0
    for cs in ROOT.rglob('*.cs'):
        if 'LEGACY' in cs.parts:
            continue
        c = process_file(cs)
        if c:
            total_files += 1
            total_changes += c
            print(f'{cs.relative_to(ROOT)}: {c} call(s)')
    print(f'\nTotal: {total_changes} call(s) across {total_files} file(s)')


if __name__ == '__main__':
    main()
