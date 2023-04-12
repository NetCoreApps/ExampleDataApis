# Adapted from https://huggingface.co/datasets/olivierdehaene/xkcd
import aiohttp
import asyncio
import re

import pandas as pd

import json

from pathlib import Path
from aiolimiter import AsyncLimiter
from typing import Dict, List
from bs4 import BeautifulSoup
from bs4.element import Tag

LIST_COMICS_500_URL = (
    "https://www.explainxkcd.com/wiki/index.php/List_of_all_comics_(1-500)"
)
LIST_COMICS_FULL_URL = (
    "https://www.explainxkcd.com/wiki/index.php/List_of_all_comics_(full)"
)


def walk_tag(initial_tag: Tag, end_tag_name: str) -> str:
    """
    Walk the HTML tree and aggregates all text between an
    initial tag and an end tag.

    Parameters
    ----------
    initial_tag: BeautifulSoup
    end_tag_name: str

    Returns
    -------
    aggregated_text: str
    """
    result = []
    current_tag = initial_tag

    # Walk the HTML
    while True:
        if current_tag.name in ["p", "dl"]:
            result.append(current_tag.get_text(separator=" ", strip=True))
        elif current_tag.name == end_tag_name:
            # We reached the end tag, break
            break
        current_tag = current_tag.next_sibling
    return "\n".join(result)


async def parse_url_html(
        session: aiohttp.ClientSession, url: str, throttler: AsyncLimiter
) -> BeautifulSoup:
    """
    Parse the HTML content of a given URL.
    The request is sent asynchronously and using a provided request throttler.
    If the request fails, we retry up to 5 times.

    Parameters
    ----------
    session: aiohttp.ClientSession
    url: str
    throttler: AsyncLimiter

    Returns
    -------
    BeautifulSoup
    """
    for _ in range(5):
        try:
            # prevent issues with rate limiters
            async with throttler:
                async with session.get(url, raise_for_status=True) as resp:
                    html = await resp.text()
            return BeautifulSoup(html, "html.parser")
        # request failed
        except aiohttp.ClientError:
            continue


async def scrap_comic(
        session: aiohttp.ClientSession, explained_url: str, throttler: AsyncLimiter
) -> Dict[str, str]:
    """
    Try to scrap all information for a given XKCD comic using its `explainxkcd.com` URL

    Parameters
    ----------
    session: aiohttp.ClientSession
    explained_url: str
    throttler: AsyncLimiter

    Returns
    -------
    Dict[str, str]
    """
    soup = await parse_url_html(session, explained_url, throttler)

    # Parse id and title
    title_splits = soup.find("h1").text.split(":")
    if len(title_splits) > 1:
        id = title_splits[0]
        title = "".join(title_splits[1:]).strip()
    else:
        id = None
        title = "".join(title_splits).strip()

    # Parse explanation
    explanation_soup = soup.find("span", {"id": "Explanation"})
    try:
        explanation = walk_tag(explanation_soup.parent, "span")
    except:
        explanation = None

    # Parse transcript
    transcript_soup = soup.find("span", {"id": "Transcript"})
    try:
        transcript = walk_tag(transcript_soup.parent, "span")
    except:
        transcript = None

    xkcd_url = f"https://www.xkcd.com/{id}"
    xkcd_soup = await parse_url_html(session, xkcd_url, throttler)

    # Parse image title
    try:
        image = xkcd_soup.find("div", {"id": "comic"}).img
        if title in image:
            image_title = image["title"]
        else:
            image_title = image["alt"]
    except:
        image_title = None

    # Parse image url
    try:
        image_url = xkcd_soup.find(text=re.compile("https://imgs.xkcd.com"))
    except:
        image_url = None

    return dict(
        id=id,
        title=title,
        image_title=image_title,
        url=xkcd_url,
        image_url=image_url,
        explained_url=explained_url,
        transcript=transcript,
        explanation=explanation,
    )


async def scap_comic_urls(
        session: aiohttp.ClientSession, comic_list_url: str
) -> List[str]:
    """
    Scrap all XKCD comic URLs from the `explainxkcd.com` website.

    Parameters
    ----------
    session: aiohttp.ClientSession
    comic_list_url: str

    Returns
    -------
    urls: List[str]
    """
    async with session.get(comic_list_url) as resp:
        html = await resp.text()
    soup = BeautifulSoup(html, "html.parser")

    # Hack to easily find comics
    create_spans = soup.find_all("span", {"class": "create"})
    return [
        "https://www.explainxkcd.com" + span.parent.a["href"] for span in create_spans
    ]


async def main():
    """
    Scrap XKCD dataset
    """
    file_path = '../../ExampleDataApis/static_data/xkcd-metadata.jsonl'
    existing_data = pd.read_json(path_or_buf=file_path, lines=True)
    start_index = existing_data.id.max()
    # Throttle to 10 requests per second
    throttler = AsyncLimiter(max_rate=10, time_period=1)
    async with aiohttp.ClientSession() as session:
        # Get all comic urls
        comic_urls = await scap_comic_urls(session, LIST_COMICS_FULL_URL)
        filtered_urls = [url for url in comic_urls if int(url.split('index.php/')[-1].split(':')[0]) > start_index]
        # Scrap all comics asynchronously
        data = await asyncio.gather(
            *[scrap_comic(session, url, throttler) for url in filtered_urls]
        )

    df = (
        pd.DataFrame.from_records(data)
        .dropna(subset=["id"])
        .astype({"id": "int32"})
        .sort_values("id")
    )
    new_comics_start = df.id.min()
    new_comics_end = df.id.max()
    df.to_json(Path(__file__).parent / f"xkcd-metadata-{new_comics_start}-{new_comics_end}.jsonl", orient="records", lines=True)


if __name__ == "__main__":
    asyncio.run(main())
