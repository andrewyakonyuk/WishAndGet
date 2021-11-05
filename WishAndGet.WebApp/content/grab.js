function grabJsonLd() {
    return Array.from(document.querySelectorAll('script[type="application/ld+json"')).map(s => s.innerText);
}

function grabMicrodata() {
    return window.Schema.scopes().map(s => JSON.stringify(s));
}

function grabData() {
    return grabMicrodata().concat(grabJsonLd());
}