#!/usr/bin/env python3
"""Conservatively add missing C# XML summaries to public declarations.
Idempotent; existing XML comments and executable statements are preserved.
"""
import pathlib
import re
import sys

TYPE = re.compile(r'^\s*public\s+(?:(?:new|sealed|static|abstract|partial|readonly|ref)\s+)*(?:class|interface|struct|enum|record(?:\s+class|\s+struct)?)\s+(\w+)\b')
MEMBER = re.compile(r'^\s*public\s+(?:(?:new|static|virtual|override|sealed|abstract|async|readonly|extern|unsafe|partial|required)\s+)*(?:[\w.<>,?\[\]]+\s+)+(\w+)\s*(\(|\{|=>|;)')
INTERFACE_MEMBER = re.compile(r'^\s*(?:(?:new|static|abstract|virtual)\s+)*(?:[\w.<>,?\[\]]+\s+)+(\w+)\s*(\(|\{|=>|;)')
EVENT = re.compile(r'\bevent\s+[\w.<>,?\[\]]+\s+(\w+)\s*[;{]')

def phrase(value):
    return re.sub(r'(?<=[a-z0-9])(?=[A-Z])', ' ', value).replace('_', ' ').lower()

def describe(name, kind):
    value = phrase(name)
    if kind == 'type': return f'Provides the {value} API.'
    if kind == 'event': return f'Occurs when {value} is raised.'
    if kind == 'property': return f'Gets or sets the {value} value.'
    for prefix, verb in [('Try','Attempts to'),('Get','Gets'),('Set','Sets'),('Can','Determines whether'),('Is','Determines whether'),('Create','Creates'),('Remove','Removes'),('Register','Registers'),('Unregister','Unregisters'),('Load','Loads'),('Save','Saves'),('Find','Finds')]:
        if name.startswith(prefix) and len(name)>len(prefix) and name[len(prefix)].isupper():
            return f'{verb} {phrase(name[len(prefix):])}.'
    return f'Performs the {value} operation.'

def run(root):
    changed_files = changed_members = 0
    for path in root.rglob('*.cs'):
        if any(part in ('obj', 'bin') for part in path.parts): continue
        raw = path.read_bytes()
        bom = raw.startswith(b'\xef\xbb\xbf')
        source = raw.decode('utf-8-sig')
        newline = '\r\n' if '\r\n' in source else '\n'
        out = []
        brace = 0
        interface_depth = None
        pending = None
        count = 0
        for line in source.splitlines(keepends=True):
            value = line.strip()
            name = kind = None
            match = TYPE.match(line)
            if match:
                name, kind = match.group(1), 'type'
                pending = 'interface' in value
            else:
                in_interface = interface_depth is not None and brace == interface_depth
                if value and not value.startswith(('/', '*', '#', '[', 'using ', 'namespace ', 'return ', 'throw ')):
                    event = EVENT.search(line) if (value.startswith('public ') or in_interface) else None
                    member = (INTERFACE_MEMBER if in_interface else MEMBER).match(line)
                    if event: name, kind = event.group(1), 'event'
                    elif member: name, kind = member.group(1), ('method' if member.group(2) == '(' else 'property')
            if name and not any(x.strip().startswith('///') for x in out[-8:]):
                indent = line[:len(line) - len(line.lstrip())]
                out.append(indent + '/// <summary>' + describe(name, kind) + '</summary>' + newline)
                count += 1
            out.append(line)
            opening, closing = line.count('{'), line.count('}')
            if match and pending and opening: interface_depth = brace + 1
            brace += opening - closing
            if match and pending and not opening: pending = 'waiting-interface'
            elif pending == 'waiting-interface' and opening:
                interface_depth = brace
                pending = None
            if interface_depth is not None and brace < interface_depth: interface_depth = None
        if count:
            path.write_bytes((b'\xef\xbb\xbf' if bom else b'') + ''.join(out).encode('utf-8'))
            changed_files += 1
            changed_members += count
    print(f'Documented {changed_members} declarations across {changed_files} files')

if __name__ == '__main__':
    run(pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else '.'))
