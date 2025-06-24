document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("pais").addEventListener("change", function () {
        var paisId = this.value;
        var provinciaSelect = document.getElementById("provincia");

        provinciaSelect.innerHTML = '<option value="">Seleccione una provincia</option>';

        if (paisId) {
            fetch(`/Cliente/ObtenerProvincias?paisId=${paisId}`)
                .then(response => {
                    if (!response.ok) throw new Error("Error en la solicitud");
                    return response.json();
                })
                .then(data => {
                    data.forEach(p => {
                        var option = document.createElement("option");
                        option.value = p.id;
                        option.text = p.nombre;
                        provinciaSelect.appendChild(option);
                    });
                })
                .catch(error => {
                    console.error("Error al cargar provincias:", error);
                });
        }
    });
});
