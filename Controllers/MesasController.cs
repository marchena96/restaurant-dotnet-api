using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Implementaciones;
using RestauranteAPI.Servicios.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MesasController : ControllerBase
    {
        private readonly IMesaService _svMesa;

        public MesasController(IMesaService svMesa)
        {
            _svMesa = svMesa;
        }
        //LISTAR TODAS LAS MESAS
        [HttpGet]
        public ActionResult<IEnumerable<MesaResponseDTO>> Get()
        {

            return Ok(_svMesa.GetAllMesas());
        }
        //BUSCAR MESA POR ID
        [HttpGet("{id}")]
        public ActionResult<MesaResponseDTO> Get(int id)
        {
            var mesa = _svMesa.GetMesaById(id);

            if (mesa == null)
            {
                return NotFound("No se encontró la mesa.");
            }

            return Ok(mesa);
        }

        //CREAR UNA MESA
        [HttpPost]
        public ActionResult<MesaResponseDTO> Post([FromBody] MesaCreateDTO nuevaMesa)
        {
            try
            {
                var result = _svMesa.CrearMesa(nuevaMesa);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        //actualizar mesa
        [HttpPut("{id}")]
        public ActionResult<MesaResponseDTO> Put(int id, [FromBody] MesaCreateDTO dto)
        {
            var result = _svMesa.ActualizarMesa(id, dto);

            if (result == null)
            {
                return NotFound("No se encontró la mesa.");
            }

            return Ok(result);
        }
        //BORRAR MESAS
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            _svMesa.EliminarMesa(id);
            return NoContent();
        }

        // validar disponibilidad de una mesa
        [HttpGet("{id}/disponibilidad/{fecha}/{horaInicio}/{horaFin}")]
        public ActionResult<bool> ValidarDisponibilidad( int id, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            var disponible = _svMesa.ValidarDisponibilidadMesa(id, fecha, horaInicio, horaFin);

            if (!disponible)
            {
                return Ok(new
                {
                    disponible = false,
                    mensaje = "La mesa no está disponible en ese horario."
                });
            }

            return Ok(new
            {
                disponible = true,
                mensaje = "La mesa está disponible."
            });
        }

        //BUSCAR MESA POR NUMERO DE MESA
        [HttpGet("numero/{numeroMesa}")]
        public ActionResult<MesaResponseDTO> BuscarMesaPorNumero(int numeroMesa)
        {
            var mesa = _svMesa.BuscarMesaPorNumero(numeroMesa);
            if (mesa == null)
                return NotFound("No se encontró la mesa.");
            return Ok(mesa);
        }

        //BUSCAR MESA POR CAPACIDAD(NUMERO DE PERSONAS)
        [HttpGet("capacidad/{capacidad:int}")]
        public IActionResult BuscarMesaPorCapacidad(int capacidad)
        {
            var mesas = _svMesa.BuscarMesaPorCapacidad(capacidad);

            if (mesas == null || !mesas.Any())
            {
                return NotFound("No se encontraron mesas con esa capacidad.");
            }

            return Ok(mesas);
        }

        //BUSCAR MESAS DISPONIBLES
        // GET api/mesas/disponibles/fecha=2026-04-24&horaInicio=19:00:00&horaFin=21:00:00

        [HttpGet("disponibles/{fecha}/{horaInicio}/{horaFin}")]
        public ActionResult<List<MesaResponseDTO>> GetMesasDisponibles(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            try
            {
                var result = _svMesa.GetMesasDisponibles(fecha, horaInicio, horaFin);

                if (result == null || result.Count == 0)
                {
                    return NotFound("No hay mesas disponibles.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }

}