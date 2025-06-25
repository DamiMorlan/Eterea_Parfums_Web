document.addEventListener("DOMContentLoaded", function () {
    var paisSelect = document.getElementById("pais");
    var provinciaSelect = document.getElementById("provincia");
    var localidadSelect = document.getElementById("localidad");
    var calleSelect = document.getElementById("calle");

    // Cuando cambie el país
    paisSelect.addEventListener("change", function () {
        var paisId = this.value;

        provinciaSelect.innerHTML = '<option value="">Seleccione una provincia</option>';
        localidadSelect.innerHTML = '<option value="">Seleccione una localidad</option>';

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

    // Cuando cambie la provincia
    provinciaSelect.addEventListener("change", function () {
        var provinciaId = this.value;

        localidadSelect.innerHTML = '<option value="">Seleccione una localidad</option>';

        if (provinciaId) {
            fetch(`/Cliente/ObtenerLocalidades?provinciaId=${provinciaId}`)
                .then(response => {
                    if (!response.ok) throw new Error("Error en la solicitud");
                    return response.json();
                })
                .then(data => {
                    data.forEach(l => {
                        var option = document.createElement("option");
                        option.value = l.id;
                        option.text = l.nombre;
                        localidadSelect.appendChild(option);
                    });
                })
                .catch(error => {
                    console.error("Error al cargar localidades:", error);
                });
        }
    });

    localidadSelect.addEventListener("change", function () {
        var localidadId = this.value;

        calleSelect.innerHTML = '<option value="">Seleccione una calle</option>';

        if (localidadId) {
            fetch(`/Cliente/ObtenerCalles?localidadId=${localidadId}`)
                .then(response => {
                    if (!response.ok) throw new Error("Error en la solicitud");
                    return response.json();
                })
                .then(data => {
                    data.forEach(c => {
                        var option = document.createElement("option");
                        option.value = c.id;
                        option.text = c.nombre;
                        calleSelect.appendChild(option);
                    });
                })
                .catch(error => {
                    console.error("Error al cargar calles:", error);
                });
        }
    });

});
