using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    public class StudentController : BaseController
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public StudentController(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }


        [HttpGet("getStudent/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStudent(Guid id)
        {
            var student = _mapper.Map<StudentDto>(await _uow.Students.FindAsync(x => x.Id == id,
                    ["Parent", "CurrentContract.Class", "CurrentContract.Class.Level", "School", "CurrentStatus"]));
            if (student is null)
                return NotFound();

            var studentAccounts = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == student.CurrentContractId)).ToList();

            student.Debit = studentAccounts.Sum(x => x.Debit - x.Credit);
            return Ok(student);
        }

    }
}
