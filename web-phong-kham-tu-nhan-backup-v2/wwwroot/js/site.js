let heroIndex = 0;
const heroSlides = document.querySelectorAll(".hero-slide");
const heroDots = document.querySelectorAll(".hero-dots .dot");

function showHeroSlide(index) {

    heroSlides.forEach(slide => slide.classList.remove("active"));
    heroDots.forEach(dot => dot.classList.remove("active"));

    heroSlides[index].classList.add("active");
    heroDots[index].classList.add("active");

    heroIndex = index;
}

function currentHeroSlide(index) {
    showHeroSlide(index);
}

function autoHeroSlide() {

    heroIndex++;

    if (heroIndex >= heroSlides.length) {
        heroIndex = 0;
    }

    showHeroSlide(heroIndex);

}

setInterval(autoHeroSlide, 5000);