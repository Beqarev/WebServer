console.log("Page script loaded successfully!");

document.addEventListener('DOMContentLoaded', (event) => {
    const pageTitle = document.title;
    console.log(`Currently on page: ${pageTitle}`);

    const header = document.querySelector('h1');
    if (header) {
        header.addEventListener('mouseover', () => {
            header.style.color = '#ff4500';
        });
        header.addEventListener('mouseout', () => {
            header.style.color = '#0056b3';
        });
    }
});