
let thumbsSwiper;
let mainGallery;

function initSwipers() {
    if (thumbsSwiper) thumbsSwiper.destroy(true, true);
    if (mainGallery) mainGallery.destroy(true, true);

    const isDesktop = window.innerWidth >= 768;

    if (isDesktop) {
        thumbsSwiper = new Swiper('.gallery-thumbs-vertical', {
            direction: 'vertical',
            spaceBetween: 10,
            slidesPerView: 2,
            watchSlidesProgress: true,
        });
    } else {
        thumbsSwiper = new Swiper('.gallery-thumbs-horizontal', {
            direction: 'horizontal',
            spaceBetween: 10,
            slidesPerView: 'auto',
            watchSlidesProgress: true,
            freeMode: true
        });
    }

    mainGallery = new Swiper('.gallery-top', {
        spaceBetween: 10,
        zoom: true,
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
        },
        thumbs: {
            swiper: thumbsSwiper
        }
    });
}

$(window).on('load', function () {
    initSwipers();
});

// Opcional: reinicializar si redimensionás la ventana
let resizeTimer;
$(window).on('resize', function () {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function () {
        initSwipers();
    }, 300);
});


$(document).ready(function () {
    new Swiper('.my-product-slider', {
        slidesPerView: 1,
        spaceBetween: 20,
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev'
        },
        pagination: {
            el: '.swiper-pagination',
            clickable: true
        },
        breakpoints: {
            576: { slidesPerView: 2 },
            768: { slidesPerView: 3 },
            992: { slidesPerView: 4 },
            1200: { slidesPerView: 5 }
        }
    });
});


function openBoxDetalle(evt, BoxDetalle) {
    var i, x, tablinks;
    x = document.getElementsByClassName("box");
    for (i = 0; i < x.length; i++) {
        x[i].style.display = "none";
    }
    tablinks = document.getElementsByClassName("tablink");
    for (i = 0; i < x.length; i++) {

        tablinks[i].className = tablinks[i].className.replace(" w3-border-pale-amber", "");
        tablinks[i].className = tablinks[i].className.replace(" w3-bottombar", "");
    }
    document.getElementById(BoxDetalle).style.display = "block";
    evt.currentTarget.firstElementChild.className += " w3-border-pale-amber w3-bottombar";

}

