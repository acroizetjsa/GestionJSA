#!/usr/bin/env python3
"""Export personnel des parcs bus/cars publics de TC Infos vers Excel.

Usage:
    python tools/tc_infos_export.py --max-network-id 2200 --delay 0.8 --output tc_infos_vehicules.xlsx

Le script ne contourne aucune authentification ni protection. Il consulte uniquement les
pages publiques /reseau/{id}/parc-bus-cars et temporise les requetes. Il peut etre relance:
les reseaux inexistants ou sans tableau sont simplement ignores.
"""
from __future__ import annotations

import argparse
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

import requests
from bs4 import BeautifulSoup
from openpyxl import Workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

BASE_URL = "https://tc-infos.fr"
USER_AGENT = "Mozilla/5.0 (compatible; PersonalDataExport/1.0; +https://github.com/acroizetjsa/GestionJSA)"

HEADERS = [
    "ReseauId",
    "Reseau",
    "NumeroParc",
    "Constructeur",
    "Modele",
    "Immatriculation",
    "MiseEnCirculation",
    "ArriveeReseau",
    "Exploitant",
    "Depot",
    "SourceURL",
    "DateExtractionUTC",
]


def clean(text: str) -> str:
    return re.sub(r"\s+", " ", text.replace("\xa0", " ")).strip()


def network_name(soup: BeautifulSoup, network_id: int) -> str:
    h1 = soup.find("h1") or soup.find("h2")
    title = clean(h1.get_text(" ", strip=True)) if h1 else ""
    title = re.sub(r"^Parc bus\s*/\s*cars\s+", "", title, flags=re.I)
    if title:
        return title
    page_title = clean(soup.title.get_text(" ", strip=True)) if soup.title else ""
    page_title = re.sub(r"^Parc bus\s*/\s*cars\s+", "", page_title, flags=re.I)
    page_title = re.sub(r"\s*-\s*TC Infos\s*$", "", page_title, flags=re.I)
    return page_title or f"Reseau {network_id}"


def parse_table(soup: BeautifulSoup, network_id: int, source_url: str) -> list[dict[str, str]]:
    table = soup.find("table")
    if table is None:
        return []

    rows = table.find_all("tr")
    if not rows:
        return []

    header_cells = rows[0].find_all(["th", "td"])
    headers = [clean(c.get_text(" ", strip=True)).lower() for c in header_cells]

    aliases = {
        "numero": ["n°", "nº", "numero", "numéro"],
        "constructeur": ["constructeur"],
        "modele": ["modèle", "modele"],
        "immat": ["immatriculation"],
        "mec": ["mise en circulation"],
        "arrivee": ["arrivée sur le réseau", "arrivee sur le reseau", "arrivée réseau"],
        "exploitant": ["exploitant"],
        "depot": ["dépôt", "depot"],
    }

    def idx_for(keys: Iterable[str]) -> int | None:
        for i, h in enumerate(headers):
            if h in keys:
                return i
        return None

    idx = {k: idx_for(v) for k, v in aliases.items()}
    # If the first row was not actually a header, infer the public TC Infos order.
    if idx["constructeur"] is None or idx["modele"] is None:
        idx = {"numero": 1, "constructeur": 2, "modele": 3, "immat": 4,
               "mec": 5, "arrivee": 6, "exploitant": 7, "depot": 8}
        data_rows = rows
    else:
        data_rows = rows[1:]

    name = network_name(soup, network_id)
    extracted = datetime.now(timezone.utc).replace(microsecond=0).isoformat()
    result: list[dict[str, str]] = []

    for tr in data_rows:
        cells = tr.find_all("td")
        if not cells:
            continue
        values = [clean(c.get_text(" ", strip=True)) for c in cells]

        def val(key: str) -> str:
            i = idx.get(key)
            return values[i] if i is not None and i < len(values) else ""

        constructeur = val("constructeur")
        modele = val("modele")
        immat = val("immat")
        numero = val("numero")
        # Ignore decorative/summary rows.
        if not any([constructeur, modele, immat, numero]):
            continue
        if constructeur.lower() == "constructeur" or modele.lower() in {"modèle", "modele"}:
            continue

        result.append({
            "ReseauId": str(network_id),
            "Reseau": name,
            "NumeroParc": numero,
            "Constructeur": constructeur,
            "Modele": modele,
            "Immatriculation": immat,
            "MiseEnCirculation": val("mec"),
            "ArriveeReseau": val("arrivee"),
            "Exploitant": val("exploitant"),
            "Depot": val("depot"),
            "SourceURL": source_url,
            "DateExtractionUTC": extracted,
        })
    return result


def fetch_network(session: requests.Session, network_id: int, timeout: float = 30.0) -> tuple[int, list[dict[str, str]]]:
    url = f"{BASE_URL}/reseau/{network_id}/parc-bus-cars"
    for attempt in range(4):
        try:
            r = session.get(url, timeout=timeout, allow_redirects=True)
            if r.status_code == 404:
                return r.status_code, []
            if r.status_code == 429:
                time.sleep(5 * (attempt + 1))
                continue
            r.raise_for_status()
            soup = BeautifulSoup(r.text, "html.parser")
            return r.status_code, parse_table(soup, network_id, url)
        except requests.RequestException:
            if attempt == 3:
                return 0, []
            time.sleep(2 * (attempt + 1))
    return 0, []


def autosize(ws) -> None:
    widths = {}
    for row in ws.iter_rows(values_only=True):
        for i, value in enumerate(row, 1):
            if value is not None:
                widths[i] = min(max(widths.get(i, 0), len(str(value)) + 2), 60)
    for i, width in widths.items():
        ws.column_dimensions[get_column_letter(i)].width = max(width, 12)


def write_xlsx(rows: list[dict[str, str]], output: Path, stats: dict[str, int]) -> None:
    wb = Workbook()
    ws = wb.active
    ws.title = "Vehicules"
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = f"A1:{get_column_letter(len(HEADERS))}1"
    ws.sheet_view.showGridLines = False

    header_fill = PatternFill("solid", fgColor="1F4E78")
    for col, title in enumerate(HEADERS, 1):
        c = ws.cell(1, col, title)
        c.font = Font(bold=True, color="FFFFFF")
        c.fill = header_fill
        c.alignment = Alignment(horizontal="center", vertical="center")

    # Stable de-duplication using the most useful public vehicle identity.
    seen = set()
    deduped = []
    for row in rows:
        key = (row["ReseauId"], row["Immatriculation"], row["NumeroParc"], row["Constructeur"], row["Modele"])
        if key in seen:
            continue
        seen.add(key)
        deduped.append(row)

    for r_idx, row in enumerate(deduped, 2):
        for c_idx, field in enumerate(HEADERS, 1):
            cell = ws.cell(r_idx, c_idx, row.get(field, ""))
            cell.font = Font(color="008000")  # imported/linked data
            cell.alignment = Alignment(vertical="top", wrap_text=False)

    autosize(ws)

    meta = wb.create_sheet("Extraction")
    meta.sheet_view.showGridLines = False
    metadata = [
        ("Source", BASE_URL),
        ("Usage", "Export personnel de pages publiques"),
        ("Date extraction UTC", datetime.now(timezone.utc).replace(microsecond=0).isoformat()),
        ("Reseaux testes", stats.get("tested", 0)),
        ("Reseaux avec vehicules", stats.get("with_rows", 0)),
        ("Lignes collectees avant dedoublonnage", len(rows)),
        ("Vehicules/lignes apres dedoublonnage", len(deduped)),
        ("Regle", "Aucune authentification/protection contournee; temporisation entre requetes."),
    ]
    for i, (k, v) in enumerate(metadata, 1):
        meta.cell(i, 1, k).font = Font(bold=True, color="666666")
        meta.cell(i, 2, v).font = Font(color="666666")
    autosize(meta)

    output.parent.mkdir(parents=True, exist_ok=True)
    wb.save(output)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--start-network-id", type=int, default=1)
    ap.add_argument("--max-network-id", type=int, default=2200)
    ap.add_argument("--delay", type=float, default=0.8, help="Pause in seconds between network pages")
    ap.add_argument("--output", default="tc_infos_vehicules.xlsx")
    args = ap.parse_args()

    session = requests.Session()
    session.headers.update({"User-Agent": USER_AGENT, "Accept-Language": "fr-FR,fr;q=0.9"})

    rows: list[dict[str, str]] = []
    stats = {"tested": 0, "with_rows": 0}
    consecutive_transport_errors = 0

    for network_id in range(args.start_network_id, args.max_network_id + 1):
        stats["tested"] += 1
        status, found = fetch_network(session, network_id)
        if status == 0:
            consecutive_transport_errors += 1
            print(f"[{network_id}] erreur transport")
            if consecutive_transport_errors >= 10:
                print("10 erreurs transport consecutives: arret de securite.")
                break
        else:
            consecutive_transport_errors = 0
        if found:
            rows.extend(found)
            stats["with_rows"] += 1
            print(f"[{network_id}] {len(found)} lignes; total={len(rows)}")
        elif network_id % 50 == 0:
            print(f"[{network_id}] progression; total={len(rows)}")
        time.sleep(max(args.delay, 0.25))

    write_xlsx(rows, Path(args.output), stats)
    print(f"Export termine: {args.output} ({len(rows)} lignes brutes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
