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

    //Script para cargar paises, provincias, localidades y calles cuando se carga la pagina
    $(document).ready(function () {
        // Al cargar la página, cargar provincias según el país seleccionado en data-selected
        var paisSeleccionado = $('#pais').val();
        if (paisSeleccionado) {
            cargarProvincias(paisSeleccionado, $('#provincia').data('selected'));
        }

        $('#pais').change(function () {
            cargarProvincias($(this).val(), null);
        });

        $('#provincia').change(function () {
            cargarLocalidades($(this).val(), null);
        });

        $('#localidad').change(function () {
            cargarCalles($(this).val(), null);
        });

        function cargarProvincias(paisId, provinciaSeleccionada) {
            $.getJSON('/Cliente/ObtenerProvincias', { paisId: paisId }, function (data) {
                var select = $('#provincia');
                select.empty();
                select.append('<option value="">Seleccione una provincia</option>');
                $.each(data, function (i, provincia) {
                    var selected = provincia.id == provinciaSeleccionada ? 'selected' : '';
                    select.append('<option value="' + provincia.id + '" ' + selected + '>' + provincia.nombre + '</option>');
                });
                if (provinciaSeleccionada) {
                    cargarLocalidades(provinciaSeleccionada, $('#localidad').data('selected'));
                }
            });
        }

        function cargarLocalidades(provinciaId, localidadSeleccionada) {
            $.getJSON('/Cliente/ObtenerLocalidades', { provinciaId: provinciaId }, function (data) {
                var select = $('#localidad');
                select.empty();
                select.append('<option value="">Seleccione una localidad</option>');
                $.each(data, function (i, localidad) {
                    var selected = localidad.id == localidadSeleccionada ? 'selected' : '';
                    select.append('<option value="' + localidad.id + '" ' + selected + '>' + localidad.nombre + '</option>');
                });
                if (localidadSeleccionada) {
                    cargarCalles(localidadSeleccionada, $('#calle').data('selected'));
                }
            });
        }

        function cargarCalles(localidadId, calleSeleccionada) {
            $.getJSON('/Cliente/ObtenerCalles', { localidadId: localidadId }, function (data) {
                var select = $('#calle');
                select.empty();
                select.append('<option value="">Seleccione una calle</option>');
                $.each(data, function (i, calle) {
                    var selected = calle.id == calleSeleccionada ? 'selected' : '';
                    select.append('<option value="' + calle.id + '" ' + selected + '>' + calle.nombre + '</option>');
                });
            });
        }
    });
});
